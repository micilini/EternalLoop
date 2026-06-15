using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using NAudio.Wave;

namespace EternalLoop.Playback.Audio;

public sealed class LoopingAudioPlayer : ILoopingAudioPlayer
{
    private readonly BeatScheduledSampleProvider _sampleProvider;
    private readonly WaveOutEvent _waveOut;
    private PlaybackState _state;
    private bool _disposed;

    public LoopingAudioPlayer(
        LoadedAudio audio,
        RuntimeTrack track,
        BranchDecisionEngine? branchDecisionEngine = null,
        BranchTransitionOptions? transitionOptions = null)
    {
        try
        {
            _sampleProvider = new BeatScheduledSampleProvider(audio, track, branchDecisionEngine, transitionOptions);
            _sampleProvider.BeatChanged += OnSampleProviderBeatChanged;
            _sampleProvider.BranchJumped += OnSampleProviderBranchJumped;
            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 80,
                NumberOfBuffers = 2
            };
            _waveOut.Init(_sampleProvider);
            _state = PlaybackState.Stopped;
        }
        catch (Exception exception) when (exception is not PlaybackException)
        {
            throw new PlaybackException("Could not initialize audio playback.", exception);
        }
    }

    public event EventHandler<BeatChangedEventArgs>? BeatChanged;

    public event EventHandler<BranchJumpEventArgs>? BranchJumped;

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    public PlaybackState State => _state;

    public int CurrentBeatIndex => _sampleProvider.CurrentBeatIndex;

    public double PositionSeconds => _sampleProvider.PositionSeconds;

    public double DurationSeconds => _sampleProvider.DurationSeconds;

    public void Play()
    {
        ThrowIfDisposed();

        if (_state == PlaybackState.Playing)
        {
            return;
        }

        try
        {
            _waveOut.Play();
            SetState(PlaybackState.Playing);
        }
        catch (Exception exception)
        {
            throw new PlaybackException("Could not start audio playback.", exception);
        }
    }

    public void Pause()
    {
        ThrowIfDisposed();

        if (_state != PlaybackState.Playing)
        {
            return;
        }

        _waveOut.Pause();
        SetState(PlaybackState.Paused);
    }

    public void Stop()
    {
        ThrowIfDisposed();

        _waveOut.Stop();
        _sampleProvider.Reset();
        SetState(PlaybackState.Stopped);
    }

    public void Seek(double seconds)
    {
        ThrowIfDisposed();
        _sampleProvider.Seek(seconds);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sampleProvider.BeatChanged -= OnSampleProviderBeatChanged;
        _sampleProvider.BranchJumped -= OnSampleProviderBranchJumped;
        _waveOut.Stop();
        _waveOut.Dispose();
        _state = PlaybackState.Disposed;
        BeatChanged = null;
        BranchJumped = null;
        StateChanged = null;
    }

    private void OnSampleProviderBeatChanged(object? sender, BeatChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        BeatChanged?.Invoke(this, e);
    }

    private void OnSampleProviderBranchJumped(object? sender, BranchJumpEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        BranchJumped?.Invoke(this, e);
    }

    private void SetState(PlaybackState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        if (!_disposed)
        {
            StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs { State = state });
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LoopingAudioPlayer));
        }
    }
}
