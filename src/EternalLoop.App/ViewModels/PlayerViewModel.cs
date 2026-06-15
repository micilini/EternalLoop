using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EternalLoop.App.Commands;
using EternalLoop.App.Services;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Runtime;
using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Visualization;

namespace EternalLoop.App.ViewModels;

public sealed class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    private const int PositionRefreshIntervalMs = 100;

    private readonly TrackRuntimePackage _runtimePackage;
    private readonly Action<string, bool> _analyzeAgain;
    private readonly Action _navigateHome;
    private readonly ITrackArtworkService _trackArtworkService;
    private readonly IAudioLoader _audioLoader;
    private readonly ILoopingAudioPlayerFactory _playerFactory;
    private readonly BranchGraphBuilder _graphBuilder;
    private readonly IAppLogger _logger;
    private readonly string _analysisSourceText;
    private readonly DispatcherTimer _positionTimer;
    private ILoopingAudioPlayer? _player;
    private bool _disposed;
    private bool _initialized;
    private bool _isInitializing;
    private bool _isSeeking;
    private BranchGraph _graph;
    private int _currentBeatIndex;
    private int _lastJumpFromBeat = -1;
    private int _lastJumpToBeat = -1;
    private ImageSource? _trackArtwork;
    private bool _hasTrackArtwork;
    private bool _isPlaying;
    private bool _isBringItHomeEnabled;
    private double _positionSeconds;
    private double _trackDurationSeconds;
    private string _positionText;
    private string _currentBeatText;
    private string _playPauseText = "Play";
    private string _analyzeAgainStatusText = string.Empty;

    public PlayerViewModel(
        TrackRuntimePackage runtimePackage,
        Action<string, bool> analyzeAgain,
        Action navigateHome,
        ITrackArtworkService trackArtworkService,
        IAudioLoader audioLoader,
        ILoopingAudioPlayerFactory playerFactory,
        BranchGraphBuilder graphBuilder,
        string analysisSourceText = "Runtime package",
        IAppLogger? logger = null)
    {
        _runtimePackage = runtimePackage
            ?? throw new ArgumentNullException(nameof(runtimePackage));
        _analyzeAgain = analyzeAgain
            ?? throw new ArgumentNullException(nameof(analyzeAgain));
        _navigateHome = navigateHome
            ?? throw new ArgumentNullException(nameof(navigateHome));
        _trackArtworkService = trackArtworkService
            ?? throw new ArgumentNullException(nameof(trackArtworkService));
        _audioLoader = audioLoader
            ?? throw new ArgumentNullException(nameof(audioLoader));
        _playerFactory = playerFactory
            ?? throw new ArgumentNullException(nameof(playerFactory));
        _graphBuilder = graphBuilder
            ?? throw new ArgumentNullException(nameof(graphBuilder));
        _logger = logger ?? NullAppLogger.Instance;
        _analysisSourceText = string.IsNullOrWhiteSpace(analysisSourceText)
            ? "Runtime package"
            : analysisSourceText;

        _graph = BuildGraph();
        _trackDurationSeconds = Math.Max(0, _runtimePackage.Metadata.DurationSeconds);
        _positionText = FormatDuration(0);
        _currentBeatText = CreateCurrentBeatText(0);

        TrackArtwork = _trackArtworkService.TryLoadArtwork(_runtimePackage.Files.AudioPath);
        HasTrackArtwork = TrackArtwork is not null;

        StopCommand = new RelayCommand(Stop);
        PlayPauseCommand = new AsyncRelayCommand(PlayPauseAsync, onError: HandleCommandError);
        BringItHomeCommand = new RelayCommand(ToggleBringItHome);
        AnalyzeAgainCommand = new RelayCommand(AnalyzeAgain);
        BeginSeekCommand = new RelayCommand(BeginSeek);
        CommitSeekCommand = new ParameterRelayCommand(CommitSeek);

        _positionTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(PositionRefreshIntervalMs)
        };
        _positionTimer.Tick += OnPositionTimerTick;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public BranchGraph Graph
    {
        get => _graph;
        private set => SetProperty(ref _graph, value);
    }

    public int CurrentBeatIndex
    {
        get => _currentBeatIndex;
        private set
        {
            if (SetProperty(ref _currentBeatIndex, value))
            {
                CurrentBeatText = CreateCurrentBeatText(value);
            }
        }
    }

    public int LastJumpFromBeat
    {
        get => _lastJumpFromBeat;
        private set => SetProperty(ref _lastJumpFromBeat, value);
    }

    public int LastJumpToBeat
    {
        get => _lastJumpToBeat;
        private set => SetProperty(ref _lastJumpToBeat, value);
    }

    public bool HasTrackArtwork
    {
        get => _hasTrackArtwork;
        private set => SetProperty(ref _hasTrackArtwork, value);
    }

    public ImageSource? TrackArtwork
    {
        get => _trackArtwork;
        private set => SetProperty(ref _trackArtwork, value);
    }

    public string DisplayTrackName => string.IsNullOrWhiteSpace(_runtimePackage.Metadata.Title)
        ? _trackArtworkService.GetDisplayTitle(_runtimePackage.Files.AudioPath)
        : _runtimePackage.Metadata.Title;

    public string TrackDurationText => FormatDuration(TrackDurationSeconds);

    public string AnalysisSourceText => _analysisSourceText;

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                PlayPauseText = value ? "Pause" : "Play";
            }
        }
    }

    public bool IsBringItHomeEnabled
    {
        get => _isBringItHomeEnabled;
        private set
        {
            if (SetProperty(ref _isBringItHomeEnabled, value))
            {
                OnPropertyChanged(nameof(BringItHomeToolTip));
                OnPropertyChanged(nameof(BringItHomeStatusText));
            }
        }
    }

    public string BringItHomeToolTip => IsBringItHomeEnabled
        ? "Bring It Home is ON: EternalLoop will stop jumping and finish the track."
        : "Bring It Home is OFF: click to let this loop finish naturally.";

    public string BringItHomeStatusText => IsBringItHomeEnabled
        ? "Finish mode: ON"
        : "Finish mode: OFF";

    public string PositionText
    {
        get => _positionText;
        private set => SetProperty(ref _positionText, value);
    }

    public double TrackDurationSeconds
    {
        get => _trackDurationSeconds;
        private set
        {
            if (SetProperty(ref _trackDurationSeconds, value))
            {
                OnPropertyChanged(nameof(TrackDurationText));
            }
        }
    }

    public double PositionSeconds
    {
        get => _positionSeconds;
        set
        {
            double clamped = ClampPosition(value);

            if (SetProperty(ref _positionSeconds, clamped))
            {
                PositionText = FormatDuration(clamped);
            }
        }
    }

    public string TempoText => _runtimePackage.Metadata.Tempo.ToString("0");

    public string CurrentBeatText
    {
        get => _currentBeatText;
        private set => SetProperty(ref _currentBeatText, value);
    }

    public string BeatCountText => _runtimePackage.Summary.RuntimeBeatCount.ToString();

    public string BranchCountText => _runtimePackage.Summary.RuntimeBranchCount.ToString();

    public string LoopModeText => $"{_runtimePackage.Tuning.Preset} loop";

    public string AnalyzeAgainStatusText
    {
        get => _analyzeAgainStatusText;
        private set => SetProperty(ref _analyzeAgainStatusText, value);
    }

    public string PlayPauseText
    {
        get => _playPauseText;
        private set => SetProperty(ref _playPauseText, value);
    }

    public ICommand StopCommand { get; }

    public ICommand PlayPauseCommand { get; }

    public ICommand BringItHomeCommand { get; }

    public ICommand AnalyzeAgainCommand { get; }

    public ICommand BeginSeekCommand { get; }

    public ICommand CommitSeekCommand { get; }

    public async Task InitializeAsync()
    {
        ThrowIfDisposed();

        if (_initialized || _isInitializing)
        {
            return;
        }

        _isInitializing = true;

        try
        {
            LoadedAudio audio = await _audioLoader
                .LoadAsync(_runtimePackage.Files.AudioPath)
                .ConfigureAwait(true);

            TrackDurationSeconds = audio.DurationSeconds > 0
                ? audio.DurationSeconds
                : _runtimePackage.Metadata.DurationSeconds;

            _player = _playerFactory.Create(
                audio,
                _runtimePackage.RuntimeTrack,
                _runtimePackage.BranchDecisionOptions);
            _player.BeatChanged += OnBeatChanged;
            _player.BranchJumped += OnBranchJumped;
            _player.StateChanged += OnStateChanged;
            _player.PlaybackCompleted += OnPlaybackCompleted;
            _initialized = true;
            AnalyzeAgainStatusText = string.Empty;
        }
        catch (Exception exception)
        {
            HandleInitializationFailure(exception);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _positionTimer.Stop();
        _positionTimer.Tick -= OnPositionTimerTick;
        IsPlaying = false;

        if (_player is not null)
        {
            _player.BeatChanged -= OnBeatChanged;
            _player.BranchJumped -= OnBranchJumped;
            _player.StateChanged -= OnStateChanged;
            _player.PlaybackCompleted -= OnPlaybackCompleted;
            TryStopAndDisposePlayer();
            _player = null;
        }

        TrackArtwork = null;
        HasTrackArtwork = false;
        Graph = BranchGraph.Empty;
        LastJumpFromBeat = -1;
        LastJumpToBeat = -1;
        CurrentBeatIndex = 0;
        PositionSeconds = 0;
        AnalyzeAgainStatusText = string.Empty;
    }

    private async Task PlayPauseAsync()
    {
        ThrowIfDisposed();

        if (!_initialized)
        {
            await InitializeAsync().ConfigureAwait(true);
        }

        if (_player is null)
        {
            return;
        }

        try
        {
            if (IsPlaying)
            {
                _player.Pause();
                return;
            }

            _player.Play();
        }
        catch (Exception exception) when (exception is PlaybackException or ObjectDisposedException or InvalidOperationException)
        {
            IsPlaying = false;
            _positionTimer.Stop();
            AnalyzeAgainStatusText = "Playback could not continue. Try stopping and starting again.";
        }
    }

    private void HandleCommandError(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return;
        }

        _logger.Log(AppLogLevel.Error, "Playback command failed.", exception);
        _positionTimer.Stop();
        IsPlaying = false;
        IsBringItHomeEnabled = false;
        AnalyzeAgainStatusText = "Playback action failed. Try stopping and starting again.";
    }

    private void Stop()
    {
        if (_disposed)
        {
            return;
        }

        if (_player is null)
        {
            PositionSeconds = 0;
            CurrentBeatIndex = 0;
            IsPlaying = false;
            return;
        }

        try
        {
            _player.Stop();
        }
        catch (Exception exception) when (exception is PlaybackException or ObjectDisposedException or InvalidOperationException)
        {
            AnalyzeAgainStatusText = "Playback was already stopped.";
        }

        PositionSeconds = 0;
        CurrentBeatIndex = 0;
        IsPlaying = false;
        IsBringItHomeEnabled = false;
    }

    private void ToggleBringItHome()
    {
        if (_disposed || _player is null)
        {
            return;
        }

        try
        {
            bool enabled = !IsBringItHomeEnabled;
            _player.SetBringItHome(enabled);
            IsBringItHomeEnabled = enabled;
        }
        catch (Exception exception) when (exception is PlaybackException or ObjectDisposedException or InvalidOperationException)
        {
            IsBringItHomeEnabled = false;
            AnalyzeAgainStatusText = "Could not change playback finish mode.";
        }
    }

    private void BeginSeek()
    {
        if (_disposed)
        {
            return;
        }

        _isSeeking = true;
    }

    private void CommitSeek(object? parameter)
    {
        if (_disposed)
        {
            return;
        }

        double target = parameter switch
        {
            double value => value,
            float value => value,
            int value => value,
            string text when double.TryParse(text, out double parsed) => parsed,
            _ => PositionSeconds
        };

        target = ClampPosition(target);
        try
        {
            _player?.Seek(target);
        }
        catch (Exception exception) when (exception is PlaybackException or ObjectDisposedException or InvalidOperationException)
        {
            AnalyzeAgainStatusText = "Seek failed. Stop and play again.";
        }

        PositionSeconds = target;
        CurrentBeatIndex = _player?.CurrentBeatIndex ?? FindBeatIndex(target);
        IsBringItHomeEnabled = false;
        _isSeeking = false;
    }

    private void AnalyzeAgain()
    {
        if (_disposed)
        {
            return;
        }

        if (!File.Exists(_runtimePackage.Files.AudioPath))
        {
            AnalyzeAgainStatusText = "Original file was not found.";
            return;
        }

        Stop();
        AnalyzeAgainStatusText = string.Empty;
        _analyzeAgain(_runtimePackage.Files.AudioPath, true);
    }

    private void HandleInitializationFailure(Exception exception)
    {
        _positionTimer.Stop();
        IsPlaying = false;
        _initialized = false;

        if (_player is not null)
        {
            _player.BeatChanged -= OnBeatChanged;
            _player.BranchJumped -= OnBranchJumped;
            _player.StateChanged -= OnStateChanged;
            _player.PlaybackCompleted -= OnPlaybackCompleted;
            TryStopAndDisposePlayer();
            _player = null;
        }

        string message = exception switch
        {
            AudioLoadException => "Audio file could not be loaded.",
            PlaybackException => "Playback could not be initialized.",
            IOException => "Audio file could not be opened.",
            UnauthorizedAccessException => "Audio file could not be accessed.",
            _ => "Playback could not be initialized."
        };

        AnalyzeAgainStatusText = $"{message} {exception.Message}";
    }

    private void OnPositionTimerTick(object? sender, EventArgs e)
    {
        if (_player is null || _isSeeking)
        {
            return;
        }

        PositionSeconds = _player.PositionSeconds;

        int beatIndex = _player.CurrentBeatIndex;
        if (beatIndex != CurrentBeatIndex)
        {
            CurrentBeatIndex = beatIndex;
        }
    }

    private void OnBeatChanged(object? sender, BeatChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        QueueOnUiThread(() =>
        {
            if (_disposed)
            {
                return;
            }

            CurrentBeatIndex = e.BeatIndex;

            if (!_isSeeking)
            {
                PositionSeconds = _player?.PositionSeconds ?? e.BeatStartSeconds;
            }
        });
    }

    private void OnBranchJumped(object? sender, BranchJumpEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        QueueOnUiThread(() =>
        {
            if (_disposed)
            {
                return;
            }

            LastJumpFromBeat = e.FromBeatIndex;
            LastJumpToBeat = e.ToBeatIndex;
        });
    }

    private void OnStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        QueueOnUiThread(() =>
        {
            if (_disposed)
            {
                return;
            }

            IsPlaying = e.State == PlaybackState.Playing;

            if (e.State == PlaybackState.Stopped)
            {
                IsBringItHomeEnabled = false;
            }

            if (IsPlaying)
            {
                _positionTimer.Start();
            }
            else
            {
                _positionTimer.Stop();
            }
        });
    }

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        QueueOnUiThread(() =>
        {
            if (_disposed)
            {
                return;
            }

            IsPlaying = false;
            IsBringItHomeEnabled = false;
            _positionTimer.Stop();
        });
    }

    private BranchGraph BuildGraph()
    {
        return _graphBuilder.Build(
            _runtimePackage.RuntimeTrack,
            new BranchGraphOptions
            {
                CurrentBeatIndex = CurrentBeatIndex,
                LastJumpFromBeat = LastJumpFromBeat,
                LastJumpToBeat = LastJumpToBeat
            });
    }

    private string CreateCurrentBeatText(int beatIndex)
    {
        int beatCount = Math.Max(0, _runtimePackage.Summary.RuntimeBeatCount);

        return beatCount == 0
            ? "0 / 0"
            : $"{Math.Clamp(beatIndex + 1, 1, beatCount)} / {beatCount}";
    }

    private int FindBeatIndex(double positionSeconds)
    {
        var beats = _runtimePackage.RuntimeTrack.Beats;

        if (beats.Count == 0)
        {
            return 0;
        }

        for (int index = 0; index < beats.Count; index++)
        {
            if (positionSeconds >= beats[index].Start
                && positionSeconds < beats[index].Start + beats[index].Duration)
            {
                return index;
            }
        }

        return positionSeconds >= beats[^1].Start ? beats.Count - 1 : 0;
    }

    private double ClampPosition(double seconds)
    {
        if (!double.IsFinite(seconds))
        {
            return 0;
        }

        return Math.Clamp(seconds, 0, Math.Max(0, TrackDurationSeconds));
    }

    private static string FormatDuration(double seconds)
    {
        TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0, seconds));

        return duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss")
            : duration.ToString(@"mm\:ss");
    }

    private static void QueueOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.BeginInvoke(action, DispatcherPriority.Render);
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void TryStopAndDisposePlayer()
    {
        try
        {
            _player?.Stop();
        }
        catch (Exception exception) when (exception is PlaybackException or ObjectDisposedException or InvalidOperationException)
        {
        }

        try
        {
            _player?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private sealed class ParameterRelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public ParameterRelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
