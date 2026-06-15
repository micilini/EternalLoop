using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using NAudio.Wave;

namespace EternalLoop.Playback.Audio;

public sealed class BeatScheduledSampleProvider : ISampleProvider
{
    private readonly object _sync = new();
    private readonly LoadedAudio _audio;
    private readonly RuntimeTrack _track;
    private readonly BranchDecisionEngine _branchDecisionEngine;
    private readonly RuntimeLinearBeatIndex _linearBeatIndex;
    private readonly BranchTransitionSmoother _transitionSmoother;
    private readonly int _totalFrames;
    private readonly int _totalSamples;
    private int _currentBeatIndex;
    private int _currentBeatStartFrame;
    private int _currentBeatEndFrame;
    private int _currentSample;
    private int _currentBeatStartSample;
    private int _currentBeatEndSample;
    private BranchTransitionKind _currentTransitionKind = BranchTransitionKind.Linear;
    private bool _pendingInitialBeatChanged = true;
    private bool _bringItHome;
    private bool _completed;

    public BeatScheduledSampleProvider(
        LoadedAudio audio,
        RuntimeTrack track,
        BranchDecisionEngine? branchDecisionEngine = null,
        BranchTransitionOptions? transitionOptions = null)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(track);

        if (audio.Samples.Length == 0)
        {
            throw new PlaybackException("Audio must contain samples.");
        }

        if (audio.SampleRate <= 0 || audio.Channels <= 0)
        {
            throw new PlaybackException("Audio format is invalid.");
        }

        if (track.Beats.Count == 0)
        {
            throw new PlaybackException("Track must contain beats.");
        }

        _audio = audio;
        _track = track;
        _linearBeatIndex = RuntimeLinearBeatIndex.FromTrack(track);
        _branchDecisionEngine = branchDecisionEngine ?? new BranchDecisionEngine();
        _transitionSmoother = new BranchTransitionSmoother(audio.SampleRate, audio.Channels, transitionOptions);
        _totalFrames = Math.Min(audio.TotalSampleFrames, audio.Samples.Length / audio.Channels);
        if (_totalFrames <= 0)
        {
            throw new PlaybackException("Audio must contain sample frames.");
        }

        _totalSamples = _totalFrames * audio.Channels;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audio.SampleRate, audio.Channels);
        SetBeat(0);
    }

    public event EventHandler<BeatChangedEventArgs>? BeatChanged;

    public event EventHandler<BranchJumpEventArgs>? BranchJumped;

    public event EventHandler? PlaybackCompleted;

    public WaveFormat WaveFormat { get; }

    public int CurrentBeatIndex
    {
        get
        {
            lock (_sync)
            {
                return _currentBeatIndex;
            }
        }
    }

    public RuntimeBeat CurrentBeat
    {
        get
        {
            lock (_sync)
            {
                return _track.Beats[_currentBeatIndex];
            }
        }
    }

    public double PositionSeconds
    {
        get
        {
            lock (_sync)
            {
                return SampleToSeconds(_currentSample);
            }
        }
    }

    public double DurationSeconds => _audio.DurationSeconds > 0
        ? _audio.DurationSeconds
        : SampleToSeconds(_totalSamples);

    public bool IsCompleted
    {
        get
        {
            lock (_sync)
            {
                return _completed;
            }
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || count < 0 || offset + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        int written = 0;
        PendingPlaybackEvents pendingEvents = default;

        lock (_sync)
        {
            if (_pendingInitialBeatChanged)
            {
                pendingEvents.AddBeatChanged(CreateBeatChangedEventArgs(_track.Beats[_currentBeatIndex]));
                _pendingInitialBeatChanged = false;
            }

            while (written < count)
            {
                if (_completed)
                {
                    Array.Clear(buffer, offset + written, count - written);
                    written = count;
                    break;
                }

                if (_currentSample >= _currentBeatEndSample || _currentSample >= _totalSamples)
                {
                    if (_bringItHome && IsLastLinearBeat())
                    {
                        _completed = true;
                        pendingEvents.AddPlaybackCompleted();
                        Array.Clear(buffer, offset + written, count - written);
                        written = count;
                        break;
                    }

                    BeatTransition transition = MoveToNextBeat();

                    if (transition.BranchJump is not null)
                    {
                        pendingEvents.AddBranchJump(transition.BranchJump);
                    }

                    pendingEvents.AddBeatChanged(transition.BeatChanged);
                }

                int beatRemainingSamples = _currentBeatEndSample - _currentSample;
                int audioRemainingSamples = _totalSamples - _currentSample;
                int copyCount = Math.Min(count - written, Math.Min(beatRemainingSamples, audioRemainingSamples));

                if (copyCount <= 0)
                {
                    buffer[offset + written] = 0;
                    written++;
                    continue;
                }

                CopySmoothedSamples(buffer, offset + written, copyCount);
                written += copyCount;
                _currentSample += copyCount;
            }
        }

        pendingEvents.Dispatch(this);

        return count;
    }

    public void SetBringItHome(bool enabled)
    {
        lock (_sync)
        {
            _bringItHome = enabled;
            _branchDecisionEngine.SetBringItHome(enabled);
        }
    }

    public void Reset()
    {
        BeatChangedEventArgs changedEvent;

        lock (_sync)
        {
            _branchDecisionEngine.Reset();
            _bringItHome = false;
            _completed = false;
            changedEvent = SetBeat(0);
            _currentTransitionKind = BranchTransitionKind.Linear;
            _pendingInitialBeatChanged = false;
        }

        BeatChanged?.Invoke(this, changedEvent);
    }

    public void Seek(double seconds)
    {
        BeatChangedEventArgs changedEvent;

        lock (_sync)
        {
            double safeSeconds = double.IsFinite(seconds) ? seconds : 0;
            safeSeconds = Math.Clamp(safeSeconds, 0, DurationSeconds);

            int targetFrame = ClampFrame(SecondsToFrame(safeSeconds));
            int targetBeatIndex = FindBeatIndexForFrame(targetFrame);
            RuntimeBeat beat = _track.Beats[targetBeatIndex];
            SetBeat(targetBeatIndex);

            int targetSample = targetFrame * _audio.Channels;
            _currentSample = Math.Clamp(targetSample, _currentBeatStartSample, Math.Max(_currentBeatStartSample, _currentBeatEndSample - 1));
            _currentTransitionKind = BranchTransitionKind.Linear;
            _pendingInitialBeatChanged = false;
            _branchDecisionEngine.Reset();
            _bringItHome = false;
            _completed = false;
            changedEvent = CreateBeatChangedEventArgs(beat);
        }

        BeatChanged?.Invoke(this, changedEvent);
    }

    private BeatTransition MoveToNextBeat()
    {
        RuntimeBeat currentBeat = _track.Beats[_currentBeatIndex];
        BranchDecisionResult decision = _branchDecisionEngine.DecideNextBeat(currentBeat, _linearBeatIndex);
        int nextBeatIndex = ResolveBeatIndex(decision.NextBeat) ?? ResolveLinearFallbackIndex(currentBeat);
        BeatChangedEventArgs beatChanged = SetBeat(nextBeatIndex);
        _currentTransitionKind = decision.UsedBranch
            ? BranchTransitionKind.BranchJump
            : BranchTransitionKind.Linear;
        BranchJumpEventArgs? branchJump = null;

        if (decision.UsedBranch
            && decision.Branch is RuntimeBranchEdge selectedBranch
            && ReferenceEquals(_track.Beats[nextBeatIndex], decision.NextBeat))
        {
            branchJump = new BranchJumpEventArgs
            {
                FromBeatIndex = decision.SourceBeat.Which,
                SeedBeatIndex = decision.SeedBeat.Which,
                ToBeatIndex = decision.NextBeat.Which,
                BranchId = selectedBranch.Id,
                Distance = selectedBranch.Distance,
                ChanceBeforeDecision = decision.ChanceBeforeDecision,
                RandomValue = decision.RandomValue,
                ForcedEndGuardJump = decision.ForcedEndGuardJump,
                Reason = decision.Reason
            };
        }

        return new BeatTransition(beatChanged, branchJump);
    }

    private void CopySmoothedSamples(float[] buffer, int destinationOffset, int copyCount)
    {
        for (int index = 0; index < copyCount; index++)
        {
            int absoluteSample = _currentSample + index;
            int currentFrame = absoluteSample / _audio.Channels;
            int beatFrame = currentFrame - _currentBeatStartFrame;
            int framesRemaining = _currentBeatEndFrame - currentFrame;
            float sample = _audio.Samples[absoluteSample];
            sample = _transitionSmoother.ApplyOutputGain(sample, framesRemaining);
            sample = _transitionSmoother.ApplyInputGain(sample, beatFrame, _currentTransitionKind);
            buffer[destinationOffset + index] = sample;
        }
    }

    private BeatChangedEventArgs SetBeat(int beatIndex)
    {
        beatIndex = Math.Clamp(beatIndex, 0, _track.Beats.Count - 1);
        RuntimeBeat beat = _track.Beats[beatIndex];
        int startFrame = ClampFrame(SecondsToFrame(beat.Start));
        int endFrame = ClampFrame(SecondsToFrame(beat.Start + beat.Duration));

        if (endFrame <= startFrame)
        {
            endFrame = Math.Min(_totalFrames, startFrame + 1);
        }

        if (endFrame <= startFrame)
        {
            startFrame = 0;
            endFrame = Math.Min(_totalFrames, 1);
        }

        _currentBeatIndex = beatIndex;
        _currentBeatStartFrame = startFrame;
        _currentBeatEndFrame = endFrame;
        _currentBeatStartSample = _currentBeatStartFrame * _audio.Channels;
        _currentBeatEndSample = _currentBeatEndFrame * _audio.Channels;
        _currentSample = _currentBeatStartSample;

        return CreateBeatChangedEventArgs(beat);
    }

    private int SecondsToFrame(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            return 0;
        }

        return (int)Math.Round(seconds * _audio.SampleRate, MidpointRounding.AwayFromZero);
    }

    private int ClampFrame(int frame)
    {
        return Math.Clamp(frame, 0, _totalFrames);
    }

    private int FindBeatIndexForFrame(int targetFrame)
    {
        double targetSeconds = targetFrame / (double)_audio.SampleRate;

        for (int index = 0; index < _track.Beats.Count; index++)
        {
            RuntimeBeat beat = _track.Beats[index];

            if (targetSeconds >= beat.Start && targetSeconds < beat.Start + beat.Duration)
            {
                return index;
            }
        }

        return targetSeconds >= _track.Beats[^1].Start
            ? _track.Beats.Count - 1
            : 0;
    }

    private int? ResolveBeatIndex(RuntimeBeat beat)
    {
        if (_linearBeatIndex.TryGetOrdinal(beat, out int ordinal))
        {
            return ordinal;
        }

        return null;
    }

    private int ResolveLinearFallbackIndex(RuntimeBeat currentBeat)
    {
        if (currentBeat.Next is not null && ResolveBeatIndex(currentBeat.Next) is int nextIndex)
        {
            return nextIndex;
        }

        return 0;
    }

    private bool IsLastLinearBeat()
    {
        RuntimeBeat currentBeat = _track.Beats[_currentBeatIndex];
        return _currentBeatIndex == _track.Beats.Count - 1 || currentBeat.Next is null;
    }

    private double SampleToSeconds(int sample)
    {
        return sample / (double)(_audio.SampleRate * _audio.Channels);
    }

    private static BeatChangedEventArgs CreateBeatChangedEventArgs(RuntimeBeat beat)
    {
        return new BeatChangedEventArgs
        {
            BeatIndex = beat.Which,
            BeatStartSeconds = beat.Start,
            BeatDurationSeconds = beat.Duration
        };
    }

    private sealed record BeatTransition(BeatChangedEventArgs BeatChanged, BranchJumpEventArgs? BranchJump);

    private struct PendingPlaybackEvents
    {
        private BeatChangedEventArgs? _firstBeatChanged;
        private BranchJumpEventArgs? _firstBranchJump;
        private List<BeatChangedEventArgs>? _additionalBeatChanges;
        private List<BranchJumpEventArgs>? _additionalBranchJumps;
        private bool _playbackCompleted;

        public void AddBeatChanged(BeatChangedEventArgs eventArgs)
        {
            if (_firstBeatChanged is null)
            {
                _firstBeatChanged = eventArgs;
                return;
            }

            _additionalBeatChanges ??= [];
            _additionalBeatChanges.Add(eventArgs);
        }

        public void AddBranchJump(BranchJumpEventArgs eventArgs)
        {
            if (_firstBranchJump is null)
            {
                _firstBranchJump = eventArgs;
                return;
            }

            _additionalBranchJumps ??= [];
            _additionalBranchJumps.Add(eventArgs);
        }

        public void AddPlaybackCompleted()
        {
            _playbackCompleted = true;
        }

        public readonly void Dispatch(BeatScheduledSampleProvider provider)
        {
            if (_firstBranchJump is not null)
            {
                provider.BranchJumped?.Invoke(provider, _firstBranchJump);
            }

            if (_additionalBranchJumps is not null)
            {
                foreach (BranchJumpEventArgs eventArgs in _additionalBranchJumps)
                {
                    provider.BranchJumped?.Invoke(provider, eventArgs);
                }
            }

            if (_firstBeatChanged is not null)
            {
                provider.BeatChanged?.Invoke(provider, _firstBeatChanged);
            }

            if (_additionalBeatChanges is not null)
            {
                foreach (BeatChangedEventArgs eventArgs in _additionalBeatChanges)
                {
                    provider.BeatChanged?.Invoke(provider, eventArgs);
                }
            }

            if (_playbackCompleted)
            {
                provider.PlaybackCompleted?.Invoke(provider, EventArgs.Empty);
            }
        }
    }
}
