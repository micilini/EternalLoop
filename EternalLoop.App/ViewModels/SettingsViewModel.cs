using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalLoop.App.Navigation;
using EternalLoop.App.Services;
using EternalLoop.Contracts;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace EternalLoop.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly AppSessionState _sessionState;
    private readonly INavigationService _navigationService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ITrackAnalysisCache _cache;
    private readonly IAppPathProvider _paths;
    private readonly ITuningService _tuningService;
    private bool _isLoadingTuning;
    private CancellationTokenSource? _autoApplyCts;
    private bool _isApplyingAutomatically;

    public string ApplicationVersion => ProductInfo.DisplayVersion;

    [ObservableProperty] private string cacheSummaryText = "Loading cache...";
    [ObservableProperty] private string cachePathText = string.Empty;
    [ObservableProperty] private bool isCacheBusy;
    [ObservableProperty] private string saveStatusText = "Settings are saved automatically.";
    [ObservableProperty] private string selectedPresetId = TuningPresetCatalog.BalancedId;
    [ObservableProperty] private string presetDescription = string.Empty;
    [ObservableProperty] private double similarityThreshold;
    [ObservableProperty] private int lookaheadDepth;
    [ObservableProperty] private int minJumpDistance;
    [ObservableProperty] private int maxBranchesPerBeat;
    [ObservableProperty] private double jumpProbability;
    [ObservableProperty] private int jumpCooldown;
    [ObservableProperty] private double firstPassLinearPlaybackRatio;
    [ObservableProperty] private bool isTuningBusy;
    [ObservableProperty] private bool isTuningDirty;
    [ObservableProperty] private string tuningStatusText = "Tuning changes are applied manually.";
    [ObservableProperty] private string tuningGraphSummaryText = "No track loaded.";
    [ObservableProperty] private bool useAiSimilarity;
    [ObservableProperty] private string aiModeStatusText = "Local AI similarity is enabled.";
    [ObservableProperty] private string aiModeDetailText = "Uses the bundled ONNX model to reduce weak musical branches.";
    [ObservableProperty] private bool canReanalyzeCurrentTrack;

    public SettingsViewModel(
        IThemeService themeService,
        AppSessionState sessionState,
        INavigationService navigationService,
        ISettingsRepository settingsRepository,
        ITrackAnalysisCache cache,
        IAppPathProvider paths,
        ITuningService tuningService)
    {
        _themeService = themeService;
        _sessionState = sessionState;
        _navigationService = navigationService;
        _settingsRepository = settingsRepository;
        _cache = cache;
        _paths = paths;
        _tuningService = tuningService;

        _sessionState.Settings.Theme = "Dark";
        _sessionState.EffectiveTheme = "Dark";
        _themeService.Apply("Dark");
        CachePathText = _paths.CacheDirectory;
        LoadTuningFromSettings();
        UpdateTuningGraphSummary();
        _ = RefreshCacheStatsAsync();
    }

    public IReadOnlyList<TuningPresetDefinition> Presets { get; } = TuningPresetCatalog.All;

    public bool IsConservativePresetSelected
    {
        get => string.Equals(SelectedPresetId, TuningPresetCatalog.ConservativeId, StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SelectedPresetId = TuningPresetCatalog.ConservativeId;
            }
        }
    }

    public bool IsBalancedPresetSelected
    {
        get => string.Equals(SelectedPresetId, TuningPresetCatalog.BalancedId, StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SelectedPresetId = TuningPresetCatalog.BalancedId;
            }
        }
    }

    public bool IsWildPresetSelected
    {
        get => string.Equals(SelectedPresetId, TuningPresetCatalog.WildId, StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SelectedPresetId = TuningPresetCatalog.WildId;
            }
        }
    }

    partial void OnSelectedPresetIdChanged(string value)
    {
        NotifyPresetSelectionChanged();

        if (_isLoadingTuning)
        {
            return;
        }

        var preset = TuningPresetCatalog.GetById(value);
        PresetDescription = preset.Description;
        _isLoadingTuning = true;
        try
        {
            SimilarityThreshold = preset.SimilarityThreshold;
            LookaheadDepth = preset.LookaheadDepth;
            MinJumpDistance = preset.MinJumpDistance;
            MaxBranchesPerBeat = preset.MaxBranchesPerBeat;
            JumpProbability = preset.JumpProbability;
            JumpCooldown = preset.JumpCooldown;
            FirstPassLinearPlaybackRatio = preset.FirstPassLinearPlaybackRatio;
        }
        finally
        {
            _isLoadingTuning = false;
        }

        IsTuningDirty = true;
        TuningStatusText = $"{preset.Name} preset selected. Applying tuning...";
        ScheduleAutoApplyTuning();
    }

    partial void OnSimilarityThresholdChanged(double value) => MarkTuningDirty();

    partial void OnLookaheadDepthChanged(int value) => MarkTuningDirty();

    partial void OnMinJumpDistanceChanged(int value) => MarkTuningDirty();

    partial void OnMaxBranchesPerBeatChanged(int value) => MarkTuningDirty();

    partial void OnJumpProbabilityChanged(double value) => MarkTuningDirty();

    partial void OnJumpCooldownChanged(int value) => MarkTuningDirty();

    partial void OnFirstPassLinearPlaybackRatioChanged(double value) => MarkTuningDirty();

    partial void OnUseAiSimilarityChanged(bool value)
    {
        if (_isLoadingTuning)
        {
            return;
        }

        _sessionState.Settings.UseAiSimilarity = value;
        UpdateAiModeStatus();
        MarkTuningDirty("AI mode changed. Applying tuning...");
    }

    [RelayCommand]
    private void Back()
    {
        if (_sessionState.CurrentResult is not null)
        {
            _navigationService.NavigateTo<PlayerViewModel>();
        }
        else
        {
            _navigationService.NavigateTo<WelcomeViewModel>();
        }
    }

    private async Task ApplyTuningAsync()
    {
        if (IsTuningBusy || _isApplyingAutomatically)
        {
            return;
        }

        IsTuningBusy = true;
        _isApplyingAutomatically = true;
        try
        {
            var settings = _sessionState.Settings;
            settings.Preset = TuningPresetCatalog.GetById(SelectedPresetId).Id;
            settings.SimilarityThreshold = Math.Clamp(SimilarityThreshold, 0.0, 1.0);
            settings.LookaheadDepth = Math.Clamp(LookaheadDepth, 1, 8);
            settings.MinJumpDistance = Math.Clamp(MinJumpDistance, 1, 128);
            settings.MaxBranchesPerBeat = Math.Clamp(MaxBranchesPerBeat, 1, 24);
            settings.JumpProbability = Math.Clamp(JumpProbability, 0.0, 1.0);
            settings.JumpCooldown = Math.Clamp(JumpCooldown, 0, 64);
            settings.FirstPassLinearPlaybackRatio = Math.Clamp(FirstPassLinearPlaybackRatio, 0.0, 0.95);
            settings.UseAiSimilarity = UseAiSimilarity;

            var result = await _tuningService.ApplyAsync(CancellationToken.None);
            TuningStatusText = result.Message;
            IsTuningDirty = false;
            UpdateTuningGraphSummary();
            UpdateAiModeStatus();
        }
        catch (Exception ex)
        {
            TuningStatusText = $"Could not apply tuning: {ex.Message}";
        }
        finally
        {
            IsTuningBusy = false;
            _isApplyingAutomatically = false;
        }
    }

    [RelayCommand]
    private void ResetBalanced()
    {
        SelectedPresetId = TuningPresetCatalog.BalancedId;
    }

    [RelayCommand]
    private void ReanalyzeCurrentTrack()
    {
        if (_sessionState.CurrentResult is null)
        {
            TuningStatusText = "No track loaded to reanalyze.";
            return;
        }

        var filePath = !string.IsNullOrWhiteSpace(_sessionState.SelectedFilePath)
            ? _sessionState.SelectedFilePath
            : _sessionState.CurrentResult.Analysis.Metadata.FilePath;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            TuningStatusText = "Original file was not found.";
            return;
        }

        _sessionState.RequestReanalysis(filePath);
        _navigationService.NavigateTo<AnalysisViewModel>();
    }

    [RelayCommand]
    private async Task RefreshCacheStatsAsync()
    {
        IsCacheBusy = true;
        try
        {
            var stats = await _cache.GetStatsAsync(CancellationToken.None);
            CacheSummaryText = $"{stats.FileCount} cached track(s), {FormatBytes(stats.TotalBytes)} used";
        }
        finally
        {
            IsCacheBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        var result = MessageBox.Show(
            "Clear all cached analyses? Songs will still be safe, but tracks will be analyzed again next time.",
            "Clear EternalLoop cache",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        IsCacheBusy = true;
        try
        {
            await _cache.ClearAsync();
            await RefreshCacheStatsAsync();
        }
        finally
        {
            IsCacheBusy = false;
        }
    }

    [RelayCommand]
    private void OpenCacheFolder()
    {
        _paths.EnsureDirectories();
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = _paths.CacheDirectory,
            UseShellExecute = true
        });
    }

    private void LoadTuningFromSettings()
    {
        _isLoadingTuning = true;
        try
        {
            var settings = _sessionState.Settings;
            SelectedPresetId = TuningPresetCatalog.GetById(settings.Preset).Id;
            SimilarityThreshold = settings.SimilarityThreshold;
            LookaheadDepth = settings.LookaheadDepth;
            MinJumpDistance = settings.MinJumpDistance;
            MaxBranchesPerBeat = settings.MaxBranchesPerBeat;
            JumpProbability = settings.JumpProbability;
            JumpCooldown = settings.JumpCooldown;
            FirstPassLinearPlaybackRatio = settings.FirstPassLinearPlaybackRatio;
            UseAiSimilarity = settings.UseAiSimilarity;
            UpdatePresetDescription();
            UpdateAiModeStatus();
            IsTuningDirty = false;
        }
        finally
        {
            _isLoadingTuning = false;
        }
    }

    private void UpdatePresetDescription()
    {
        PresetDescription = TuningPresetCatalog.GetById(SelectedPresetId).Description;
        NotifyPresetSelectionChanged();
    }

    private void NotifyPresetSelectionChanged()
    {
        OnPropertyChanged(nameof(IsConservativePresetSelected));
        OnPropertyChanged(nameof(IsBalancedPresetSelected));
        OnPropertyChanged(nameof(IsWildPresetSelected));
    }

    private void MarkTuningDirty()
    {
        MarkTuningDirty("Applying tuning...");
    }

    private void MarkTuningDirty(string statusText)
    {
        if (_isLoadingTuning)
        {
            return;
        }

        IsTuningDirty = true;
        TuningStatusText = statusText;
        ScheduleAutoApplyTuning();
    }

    private void ScheduleAutoApplyTuning()
    {
        _autoApplyCts?.Cancel();
        _autoApplyCts?.Dispose();

        _autoApplyCts = new CancellationTokenSource();
        var token = _autoApplyCts.Token;

        _ = AutoApplyTuningAsync(token);
    }

    private async Task AutoApplyTuningAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(450, token).ConfigureAwait(false);
            var applyTask = await Application.Current.Dispatcher.InvokeAsync(ApplyTuningAsync);
            await applyTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void UpdateTuningGraphSummary()
    {
        var result = _sessionState.CurrentResult;
        if (result is null)
        {
            TuningGraphSummaryText = "No track loaded. Changes will apply to the next track.";
            return;
        }

        var branches = result.Graph.JumpEdges.Sum(pair => pair.Value.Count);
        var modeText = result.AiRun.Status switch
        {
            AiAnalysisRunStatus.FailedFallback => "Classic fallback after AI failure.",
            _ when UseAiSimilarity => "AI mode enabled.",
            _ => "Classic DSP mode."
        };

        TuningGraphSummaryText = $"Current track: {branches} branch(es). {modeText}";
    }

    private void UpdateAiModeStatus()
    {
        var result = _sessionState.CurrentResult;
        if (result is null)
        {
            AiModeStatusText = UseAiSimilarity ? "AI mode is enabled." : "AI mode is disabled.";
            AiModeDetailText = UseAiSimilarity
                ? "The next track will run local AI similarity and cache beat embeddings."
                : "The next track will use the faster classic DSP analysis only.";
            CanReanalyzeCurrentTrack = false;
            return;
        }

        CanReanalyzeCurrentTrack = true;

        if (result.AiRun.FellBackToClassic)
        {
            AiModeStatusText = "AI fallback was used for this track.";
            AiModeDetailText = "Local AI failed during analysis. The player is using classic DSP. Diagnostics remain available in the player.";
            return;
        }

        if (result.Analysis.Ai is not null)
        {
            AiModeStatusText = UseAiSimilarity
                ? "AI mode is enabled for this track."
                : "AI mode is disabled for this track.";
            AiModeDetailText = UseAiSimilarity
                ? "The current loop graph can be rebuilt with cached AI beat embeddings."
                : "Applying settings rebuilds the graph with classic DSP only. Cached AI data remains available.";
            return;
        }

        AiModeStatusText = UseAiSimilarity
            ? "AI mode is enabled, but this track has no AI embeddings yet."
            : "AI mode is disabled for this track.";
        AiModeDetailText = UseAiSimilarity
            ? "Reanalyze the current track to generate local AI embeddings."
            : "This track is using classic DSP analysis.";
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsRepository.SaveAsync(_sessionState.Settings, CancellationToken.None);
            SaveStatusText = "Settings saved.";
        }
        catch (Exception ex)
        {
            SaveStatusText = $"Could not save settings: {ex.Message}";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}
