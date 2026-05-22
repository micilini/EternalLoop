using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlaybackState = EternalLoop.Contracts.Enums.PlaybackState;

namespace EternalLoop.Core.Playback;

public sealed class NAudioStreamPlayer : IAudioPlayer, IDisposable
{
    private const float DefaultVolume = 0.8f;
    private const int DefaultDesiredLatencyMilliseconds = 100;

    private readonly ILogger<NAudioStreamPlayer> _logger;
    private readonly PlaybackOptions _options;
    private readonly Func<IWavePlayer> _outputFactory;
    private readonly object _syncRoot = new();

    private IWavePlayer? _output;
    private JukeboxSampleProvider? _provider;
    private PlaybackState _state = PlaybackState.Stopped;
    private float _volume = DefaultVolume;
    private bool _disposed;

    public NAudioStreamPlayer(
        IOptions<PlaybackOptions> options,
        ILogger<NAudioStreamPlayer> logger)
        : this(options, logger, () => new WaveOutEvent { DesiredLatency = DefaultDesiredLatencyMilliseconds })
    {
    }

    public NAudioStreamPlayer(
        IOptions<PlaybackOptions> options,
        ILogger<NAudioStreamPlayer> logger,
        Func<IWavePlayer> outputFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(outputFactory);

        _options = options.Value;
        _logger = logger;
        _outputFactory = outputFactory;
    }

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    public event EventHandler<BeatChangedEventArgs>? BeatChanged;

    public TimeSpan Position
    {
        get
        {
            lock (_syncRoot)
            {
                return _provider?.Position ?? TimeSpan.Zero;
            }
        }
    }

    public float Volume
    {
        get
        {
            lock (_syncRoot)
            {
                return _volume;
            }
        }
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);

            lock (_syncRoot)
            {
                _volume = clamped;
                if (_output is not null)
                {
                    _output.Volume = clamped;
                }
            }
        }
    }

    public Task LoadAsync(
        LoadedAudio audio,
        IJukeboxEngine engine,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(engine);

        lock (_syncRoot)
        {
            SetStateUnsafe(PlaybackState.Loading, "Loading audio player");

            DisposeCurrentOutputUnsafe();

            _provider = new JukeboxSampleProvider(audio, engine, _options);
            _provider.BeatChanged += OnProviderBeatChanged;

            _output = _outputFactory();
            _output.Volume = _volume;
            _output.PlaybackStopped += OnPlaybackStopped;
            _output.Init(_provider.ToWaveProvider());

            SetStateUnsafe(PlaybackState.Stopped, "Audio player loaded");
        }

        _logger.LogInformation(
            "Audio player loaded: {SampleCount} samples at {SampleRate} Hz",
            audio.Samples.Length,
            audio.SampleRate);

        return Task.CompletedTask;
    }

    public void Play()
    {
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            EnsureLoadedUnsafe();
            _output!.Play();
            SetStateUnsafe(PlaybackState.Playing);
        }
    }

    public void Pause()
    {
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            EnsureLoadedUnsafe();

            if (_state != PlaybackState.Playing)
            {
                return;
            }

            _output!.Pause();
            SetStateUnsafe(PlaybackState.Paused);
        }
    }

    public void Stop()
    {
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            EnsureLoadedUnsafe();
            _output!.Stop();
            _provider!.Reset();
            SetStateUnsafe(PlaybackState.Stopped);
        }
    }

    public void Seek(TimeSpan position)
    {
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            EnsureLoadedUnsafe();
            _provider!.Seek(position);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_syncRoot)
        {
            DisposeCurrentOutputUnsafe();
            _disposed = true;
        }
    }

    private void OnProviderBeatChanged(object? sender, BeatChangedEventArgs args)
    {
        BeatChanged?.Invoke(this, args);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs args)
    {
        if (args.Exception is null)
        {
            return;
        }

        _logger.LogError(args.Exception, "Audio playback stopped with an error");

        lock (_syncRoot)
        {
            SetStateUnsafe(PlaybackState.Error, args.Exception.Message);
        }
    }

    private void DisposeCurrentOutputUnsafe()
    {
        if (_provider is not null)
        {
            _provider.BeatChanged -= OnProviderBeatChanged;
            _provider = null;
        }

        if (_output is not null)
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Dispose();
            _output = null;
        }
    }

    private void EnsureLoadedUnsafe()
    {
        if (_output is null || _provider is null)
        {
            throw new InvalidOperationException("Audio player is not loaded.");
        }
    }

    private void SetStateUnsafe(PlaybackState newState, string? message = null)
    {
        var oldState = _state;
        if (oldState == newState && message is null)
        {
            return;
        }

        _state = newState;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(oldState, newState, message));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
