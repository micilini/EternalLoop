using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using NAudio.Wave;

namespace EternalLoop.Core.Playback;

public sealed class JukeboxSampleProvider : ISampleProvider
{
    private readonly float[] _samples;
    private readonly IJukeboxEngine _engine;
    private readonly IReadOnlyList<Beat> _beats;
    private readonly PlaybackOptions _options;
    private readonly int _sampleRate;
    private readonly int _crossfadeSamples;

    private int _currentBeatIndex;
    private int _positionWithinBeatSamples;
    private float[]? _pendingCrossfade;
    private int _pendingCrossfadePosition;

    public JukeboxSampleProvider(
        LoadedAudio audio,
        IJukeboxEngine engine,
        PlaybackOptions options)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(options);

        if (audio.Samples.Length == 0)
        {
            throw new ArgumentException("Loaded audio must contain samples.", nameof(audio));
        }

        _beats = engine.Beats;
        if (_beats.Count == 0)
        {
            throw new ArgumentException("The jukebox engine must expose at least one beat.", nameof(engine));
        }

        _samples = audio.Samples;
        _sampleRate = audio.SampleRate;
        _engine = engine;
        _options = options;
        _crossfadeSamples = Math.Max(0, audio.SampleRate * options.CrossfadeMilliseconds / 1000);

        _currentBeatIndex = Math.Clamp(engine.GetCurrentBeatIndex(), 0, _beats.Count - 1);

        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audio.SampleRate, 1);
    }

    public event EventHandler<BeatChangedEventArgs>? BeatChanged;

    public WaveFormat WaveFormat { get; }

    public TimeSpan Position
    {
        get
        {
            var beat = _beats[_currentBeatIndex];
            var absoluteSample = BeatStartSample(beat) + _positionWithinBeatSamples;
            return TimeSpan.FromSeconds(absoluteSample / (double)_sampleRate);
        }
    }

    public int CurrentBeatIndex => _currentBeatIndex;

    public int Read(float[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || count < 0 || offset + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Invalid offset/count for the target buffer.");
        }

        var written = 0;

        while (written < count)
        {
            var beat = _beats[_currentBeatIndex];
            var beatStartSample = BeatStartSample(beat);
            var beatLengthSamples = BeatLengthSamples(beat);
            var remainingInBeat = beatLengthSamples - _positionWithinBeatSamples;

            if (remainingInBeat <= 0)
            {
                MoveToNextBeat();
                continue;
            }

            var toWrite = Math.Min(remainingInBeat, count - written);
            var sourceOffset = beatStartSample + _positionWithinBeatSamples;

            CopySamplesOrSilence(sourceOffset, buffer, offset + written, toWrite);
            ApplyPendingCrossfade(buffer, offset + written, toWrite);

            written += toWrite;
            _positionWithinBeatSamples += toWrite;

            if (_positionWithinBeatSamples >= beatLengthSamples)
            {
                MoveToNextBeat();
            }
        }

        return written;
    }

    public void Reset()
    {
        _engine.Reset();
        _currentBeatIndex = Math.Clamp(_engine.GetCurrentBeatIndex(), 0, _beats.Count - 1);
        _positionWithinBeatSamples = 0;
        _pendingCrossfade = null;
        _pendingCrossfadePosition = 0;

        RaiseBeatChanged();
    }

    public void Seek(TimeSpan position)
    {
        var targetSeconds = Math.Max(0, position.TotalSeconds);
        var beatIndex = FindBeatIndexForTime(targetSeconds);
        var beat = _beats[beatIndex];

        _engine.SeekToBeat(beatIndex);
        _currentBeatIndex = beatIndex;
        _positionWithinBeatSamples = Math.Clamp(
            (int)Math.Round((targetSeconds - beat.Start) * _sampleRate),
            0,
            Math.Max(0, BeatLengthSamples(beat) - 1));

        _pendingCrossfade = null;
        _pendingCrossfadePosition = 0;

        RaiseBeatChanged();
    }

    private void MoveToNextBeat()
    {
        var previousBeatIndex = _currentBeatIndex;
        var nextBeatIndex = _engine.AdvanceToNextBeat();

        _currentBeatIndex = Math.Clamp(nextBeatIndex, 0, _beats.Count - 1);
        _positionWithinBeatSamples = 0;

        if (IsNonContiguousTransition(previousBeatIndex, _currentBeatIndex))
        {
            PrepareCrossfade(previousBeatIndex);
        }

        RaiseBeatChanged();
    }

    private static bool IsNonContiguousTransition(int previousBeatIndex, int nextBeatIndex)
    {
        return nextBeatIndex != previousBeatIndex + 1;
    }

    private void PrepareCrossfade(int fromBeatIndex)
    {
        if (_crossfadeSamples <= 0)
        {
            _pendingCrossfade = null;
            _pendingCrossfadePosition = 0;
            return;
        }

        var fromBeat = _beats[Math.Clamp(fromBeatIndex, 0, _beats.Count - 1)];
        var endSample = BeatStartSample(fromBeat) + BeatLengthSamples(fromBeat);
        var startSample = Math.Max(0, endSample - _crossfadeSamples);
        var actualLength = Math.Min(_crossfadeSamples, Math.Max(0, _samples.Length - startSample));

        if (actualLength <= 0)
        {
            _pendingCrossfade = null;
            _pendingCrossfadePosition = 0;
            return;
        }

        _pendingCrossfade = new float[actualLength];
        _samples
            .AsSpan(startSample, actualLength)
            .CopyTo(_pendingCrossfade.AsSpan(0, actualLength));
        _pendingCrossfadePosition = 0;
    }

    private void ApplyPendingCrossfade(float[] buffer, int offset, int count)
    {
        if (_pendingCrossfade is null)
        {
            return;
        }

        var target = buffer.AsSpan(offset, count);
        CrossfadeBuffer.Mix(
            _pendingCrossfade,
            target,
            _pendingCrossfadePosition,
            _options.Shape);

        _pendingCrossfadePosition += Math.Min(count, _pendingCrossfade.Length - _pendingCrossfadePosition);

        if (_pendingCrossfadePosition >= _pendingCrossfade.Length)
        {
            _pendingCrossfade = null;
            _pendingCrossfadePosition = 0;
        }
    }

    private void CopySamplesOrSilence(int sourceOffset, float[] buffer, int targetOffset, int count)
    {
        if (count <= 0)
        {
            return;
        }

        if (targetOffset < 0 || targetOffset + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(targetOffset), "Invalid target range for playback buffer.");
        }

        if (sourceOffset < 0)
        {
            var missing = Math.Min(count, -sourceOffset);
            Array.Clear(buffer, targetOffset, missing);
            sourceOffset = 0;
            targetOffset += missing;
            count -= missing;

            if (count <= 0)
            {
                return;
            }
        }

        if (sourceOffset >= _samples.Length)
        {
            Array.Clear(buffer, targetOffset, count);
            return;
        }

        var available = Math.Min(count, _samples.Length - sourceOffset);

        _samples
            .AsSpan(sourceOffset, available)
            .CopyTo(buffer.AsSpan(targetOffset, available));

        if (available < count)
        {
            Array.Clear(buffer, targetOffset + available, count - available);
        }
    }

    private int BeatStartSample(Beat beat)
    {
        return Math.Max(0, (int)Math.Round(beat.Start * _sampleRate));
    }

    private int BeatLengthSamples(Beat beat)
    {
        var samples = (int)Math.Round(beat.Duration * _sampleRate);
        return Math.Max(1, samples);
    }

    private int FindBeatIndexForTime(double seconds)
    {
        if (_beats.Count == 0)
        {
            return 0;
        }

        var low = 0;
        var high = _beats.Count - 1;
        var best = 0;

        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            if (_beats[mid].Start <= seconds)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return Math.Clamp(best, 0, _beats.Count - 1);
    }

    private void RaiseBeatChanged()
    {
        var beat = _beats[_currentBeatIndex];
        BeatChanged?.Invoke(this, new BeatChangedEventArgs(
            _currentBeatIndex,
            beat.Start,
            beat.Duration));
    }
}
