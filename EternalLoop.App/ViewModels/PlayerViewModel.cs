using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalLoop.App.Navigation;
using EternalLoop.App.Services;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EternalLoop.App.ViewModels;

public partial class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly AppSessionState _sessionState;
    private readonly IAudioPlayer _player;
    private readonly IJukeboxEngine _engine;
    private readonly INavigationService _navigationService;
    private readonly ITrackArtworkService _trackArtworkService;
    private readonly DispatcherTimer _timer;

    [ObservableProperty] private JukeboxGraph? graph;
    [ObservableProperty] private string trackName = "No track";
    [ObservableProperty] private string tempoText = "-- BPM";
    [ObservableProperty] private string beatCountText = "0 beats";
    [ObservableProperty] private string branchCountText = "0 branches";
    [ObservableProperty] private string positionText = "00:00";
    [ObservableProperty] private int currentBeatIndex;
    [ObservableProperty] private int lastJumpFromBeat = -1;
    [ObservableProperty] private int lastJumpToBeat = -1;
    [ObservableProperty] private bool isPlaying;
    [ObservableProperty] private string playPauseText = "Play";
    [ObservableProperty] private string analysisQualityText = "Analysis quality: unknown";
    [ObservableProperty] private string currentBeatText = "Beat -- / --";
    [ObservableProperty] private ImageSource? trackArtwork;
    [ObservableProperty] private bool hasTrackArtwork;
    [ObservableProperty] private string displayTrackName = "Loaded track";
    [ObservableProperty] private string trackDurationText = "--:--";
    [ObservableProperty] private string loopModeText = "Safe loop";
    [ObservableProperty] private double positionSeconds;
    [ObservableProperty] private double trackDurationSeconds;
    [ObservableProperty] private bool isSeeking;
    [ObservableProperty] private string analysisSourceText = "Fresh analysis";
    [ObservableProperty] private string cacheBadgeText = "Fresh";
    [ObservableProperty] private string cacheDetailText = "Analysis saved locally.";
    [ObservableProperty] private string analyzeAgainStatusText = string.Empty;
    [ObservableProperty] private ObservableCollection<LoopBranchPreview> branchPreviews = new();

    public PlayerViewModel(
        AppSessionState sessionState,
        IAudioPlayer player,
        IJukeboxEngine engine,
        INavigationService navigationService,
        ITrackArtworkService trackArtworkService)
    {
        _sessionState = sessionState;
        _player = player;
        _engine = engine;
        _navigationService = navigationService;
        _trackArtworkService = trackArtworkService;

        _player.BeatChanged += OnBeatChanged;
        _player.StateChanged += OnStateChanged;
        _engine.JumpOccurred += OnJumpOccurred;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) =>
        {
            var position = _player.Position;
            PositionText = FormatTime(position);

            if (!IsSeeking)
            {
                PositionSeconds = Math.Clamp(position.TotalSeconds, 0, TrackDurationSeconds);
            }
        };
        _timer.Start();

        LoadFromSession();
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (IsPlaying)
        {
            _player.Pause();
        }
        else
        {
            _player.Play();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _player.Stop();
        CurrentBeatIndex = 0;
        PositionText = "00:00";
        PositionSeconds = 0;
        UpdateCurrentBeatText();
    }

    [RelayCommand]
    private void OpenAnother()
    {
        _player.Stop();
        _navigationService.NavigateTo<WelcomeViewModel>();
    }

    [RelayCommand]
    private void Settings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
    }

    [RelayCommand]
    private void AnalyzeAgain()
    {
        var filePath = _sessionState.SelectedFilePath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            filePath = _sessionState.CurrentResult?.Analysis.Metadata.FilePath;
        }

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            AnalyzeAgainStatusText = "Original file was not found.";
            return;
        }

        try
        {
            _player.Stop();
        }
        catch (InvalidOperationException)
        {
        }

        IsPlaying = false;
        PlayPauseText = "Play";
        PositionText = "00:00";
        AnalyzeAgainStatusText = string.Empty;

        _sessionState.RequestReanalysis(filePath);
        _navigationService.NavigateTo<AnalysisViewModel>();
    }

    [RelayCommand]
    private void BeginSeek()
    {
        IsSeeking = true;
    }

    [RelayCommand]
    private void CommitSeek(double seconds)
    {
        var clamped = Math.Clamp(seconds, 0, TrackDurationSeconds);
        _player.Seek(TimeSpan.FromSeconds(clamped));
        PositionSeconds = clamped;
        PositionText = FormatTime(TimeSpan.FromSeconds(clamped));
        UpdateCurrentBeatText();
        IsSeeking = false;
    }

    private void LoadFromSession()
    {
        var result = _sessionState.CurrentResult;
        if (result is null)
        {
            _navigationService.NavigateTo<WelcomeViewModel>();
            return;
        }

        Graph = result.Graph;
        _player.Volume = 1.0f;
        var filePath = _sessionState.SelectedFilePath;
        DisplayTrackName = _trackArtworkService.GetDisplayTitle(filePath);
        TrackName = Path.GetFileName(filePath) ?? "Loaded track";
        TrackArtwork = _trackArtworkService.TryLoadArtwork(filePath);
        HasTrackArtwork = TrackArtwork is not null;
        TrackDurationSeconds = result.Analysis.Metadata.DurationSeconds;
        PositionSeconds = 0;
        TrackDurationText = FormatTime(TimeSpan.FromSeconds(TrackDurationSeconds));
        LoopModeText = $"{_sessionState.Settings.Preset} loop";
        UpdateCacheInfo(result);
        TempoText = $"{result.Analysis.Metadata.Tempo:0.0}";
        var beatCount = result.Analysis.Beats.Count;
        var branchCount = result.Graph.JumpEdges.Sum(pair => pair.Value.Count);
        BeatCountText = beatCount.ToString(CultureInfo.CurrentCulture);
        BranchCountText = branchCount.ToString(CultureInfo.CurrentCulture);
        AnalysisQualityText = beatCount switch
        {
            < 8 => "Analysis quality: too few beats detected",
            _ when branchCount == 0 => "Analysis quality: no branches found",
            _ => "Analysis quality: ready"
        };
        CurrentBeatIndex = _engine.GetCurrentBeatIndex();
        LoadBranchPreviews(result.Graph);
        UpdateCurrentBeatText();
    }

    private void OnBeatChanged(object? sender, BeatChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentBeatIndex = e.BeatIndex;
            PositionText = FormatTime(TimeSpan.FromSeconds(e.BeatStart));
            UpdateCurrentBeatText();
        });
    }

    private void OnJumpOccurred(object? sender, JumpEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LastJumpFromBeat = e.FromBeat;
            LastJumpToBeat = e.ToBeat;
        });
    }

    private void OnStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsPlaying = e.NewState == PlaybackState.Playing;
            PlayPauseText = IsPlaying ? "Pause" : "Play";
        });
    }

    private static string FormatTime(TimeSpan position)
        => position.TotalHours >= 1
            ? position.ToString(@"hh\:mm\:ss")
            : position.ToString(@"mm\:ss");

    private void UpdateCurrentBeatText()
    {
        var total = Graph?.Nodes.Count ?? 0;
        if (total <= 0)
        {
            CurrentBeatText = "-- / --";
            return;
        }

        CurrentBeatText = $"{Math.Clamp(CurrentBeatIndex + 1, 1, total)} / {total}";
    }

    private void LoadBranchPreviews(JukeboxGraph graph)
    {
        BranchPreviews.Clear();

        var previews = graph.JumpEdges
            .SelectMany(pair => pair.Value)
            .OrderByDescending(edge => edge.Similarity)
            .Take(4)
            .Select(edge => new LoopBranchPreview
            {
                Label = $"Beat {edge.FromBeat + 1} -> {edge.ToBeat + 1}",
                SimilarityText = edge.Similarity.ToString("0.00")
            });

        foreach (var preview in previews)
        {
            BranchPreviews.Add(preview);
        }
    }

    private void UpdateCacheInfo(JukeboxAnalysisResult result)
    {
        var analyzedAt = result.Analysis.Metadata.AnalyzedAt;
        if (analyzedAt.Kind == DateTimeKind.Unspecified)
        {
            analyzedAt = DateTime.SpecifyKind(analyzedAt, DateTimeKind.Utc);
        }

        var local = analyzedAt.ToLocalTime();
        var formatted = local.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

        if (result.LoadedFromCache)
        {
            AnalysisSourceText = "Loaded from cache";
            CacheBadgeText = "Cache hit";
            CacheDetailText = $"Cached analysis from {formatted}";
            return;
        }

        AnalysisSourceText = "Fresh analysis";
        CacheBadgeText = "Saved now";
        CacheDetailText = $"Analysis saved to cache on {formatted}";
    }

    public void Dispose()
    {
        _timer.Stop();
        _player.BeatChanged -= OnBeatChanged;
        _player.StateChanged -= OnStateChanged;
        _engine.JumpOccurred -= OnJumpOccurred;
    }
}
