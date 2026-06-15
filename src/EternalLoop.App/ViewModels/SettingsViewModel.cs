using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using EternalLoop.App.Commands;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Settings;

namespace EternalLoop.App.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly Action _back;
    private readonly EternalLoopUserSettings _userSettings;
    private readonly IUserSettingsRepository _settingsRepository;
    private readonly IAnalysisCacheService _cacheService;
    private readonly IAppPathProvider _pathProvider;
    private readonly IRecentTracksService _recentTracksService;
    private bool _isLoadingTuning;
    private CancellationTokenSource? _autoApplyCts;
    private bool _isApplyingAutomatically;
    private string _cacheSummaryText = "Loading cache...";
    private string _cachePathText;
    private bool _isCacheBusy;
    private string _selectedPresetId = LoopTuningPresetCatalog.BalancedId;
    private double _similarityThreshold;
    private int _lookaheadDepth;
    private int _minJumpDistance;
    private int _maxBranchesPerBeat;
    private double _jumpProbability;
    private int _jumpCooldown;
    private double _firstPassLinearPlaybackRatio;
    private bool _isTuningBusy;
    private bool _isTuningDirty;
    private string _tuningStatusText = "Tuning changes are saved automatically.";
    private string _tuningGraphSummaryText = "No track loaded. Changes will apply to the next track.";

    public SettingsViewModel(
        Action back,
        EternalLoopUserSettings userSettings,
        IUserSettingsRepository settingsRepository,
        IAnalysisCacheService cacheService,
        IAppPathProvider pathProvider,
        IRecentTracksService recentTracksService)
    {
        _back = back ?? throw new ArgumentNullException(nameof(back));
        _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
        _settingsRepository = settingsRepository
            ?? throw new ArgumentNullException(nameof(settingsRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _recentTracksService = recentTracksService
            ?? throw new ArgumentNullException(nameof(recentTracksService));
        _cachePathText = _pathProvider.CacheDirectory;

        BackCommand = new RelayCommand(_back);
        ResetBalancedCommand = new AsyncRelayCommand(ResetBalancedAsync);
        RefreshCacheStatsCommand = new AsyncRelayCommand(RefreshCacheStatsAsync);
        OpenCacheFolderCommand = new AsyncRelayCommand(OpenCacheFolderAsync);
        ClearCacheCommand = new AsyncRelayCommand(ClearCacheAsync);

        LoadFromSettings(_userSettings.Tuning);
        _ = RefreshCacheStatsAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ApplicationVersion => $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";

    public string CacheSummaryText
    {
        get => _cacheSummaryText;
        private set => SetProperty(ref _cacheSummaryText, value);
    }

    public string CachePathText
    {
        get => _cachePathText;
        private set => SetProperty(ref _cachePathText, value);
    }

    public bool IsCacheBusy
    {
        get => _isCacheBusy;
        private set => SetProperty(ref _isCacheBusy, value);
    }

    public string SelectedPresetId
    {
        get => _selectedPresetId;
        private set
        {
            if (SetProperty(ref _selectedPresetId, value))
            {
                OnPropertyChanged(nameof(PresetDescription));
                OnPropertyChanged(nameof(IsConservativePresetSelected));
                OnPropertyChanged(nameof(IsBalancedPresetSelected));
                OnPropertyChanged(nameof(IsWildPresetSelected));
            }
        }
    }

    public string PresetDescription =>
        LoopTuningPresetCatalog.GetById(SelectedPresetId).Description;

    public double SimilarityThreshold
    {
        get => _similarityThreshold;
        set
        {
            if (SetProperty(ref _similarityThreshold, Math.Round(value, 2)))
            {
                MarkTuningDirty();
            }
        }
    }

    public int LookaheadDepth
    {
        get => _lookaheadDepth;
        set
        {
            if (SetProperty(ref _lookaheadDepth, value))
            {
                MarkTuningDirty();
            }
        }
    }

    public int MinJumpDistance
    {
        get => _minJumpDistance;
        set
        {
            if (SetProperty(ref _minJumpDistance, value))
            {
                MarkTuningDirty();
            }
        }
    }

    public int MaxBranchesPerBeat
    {
        get => _maxBranchesPerBeat;
        set
        {
            if (SetProperty(ref _maxBranchesPerBeat, value))
            {
                MarkTuningDirty();
            }
        }
    }

    public double JumpProbability
    {
        get => _jumpProbability;
        set
        {
            if (SetProperty(ref _jumpProbability, Math.Round(value, 2)))
            {
                MarkTuningDirty();
            }
        }
    }

    public int JumpCooldown
    {
        get => _jumpCooldown;
        set
        {
            if (SetProperty(ref _jumpCooldown, value))
            {
                MarkTuningDirty();
            }
        }
    }

    public double FirstPassLinearPlaybackRatio
    {
        get => _firstPassLinearPlaybackRatio;
        set
        {
            if (SetProperty(ref _firstPassLinearPlaybackRatio, Math.Round(value, 2)))
            {
                MarkTuningDirty();
            }
        }
    }

    public bool IsTuningBusy
    {
        get => _isTuningBusy;
        private set => SetProperty(ref _isTuningBusy, value);
    }

    public bool IsTuningDirty
    {
        get => _isTuningDirty;
        private set => SetProperty(ref _isTuningDirty, value);
    }

    public string TuningStatusText
    {
        get => _tuningStatusText;
        private set => SetProperty(ref _tuningStatusText, value);
    }

    public string TuningGraphSummaryText
    {
        get => _tuningGraphSummaryText;
        private set => SetProperty(ref _tuningGraphSummaryText, value);
    }

    public bool IsConservativePresetSelected
    {
        get => string.Equals(SelectedPresetId, LoopTuningPresetCatalog.ConservativeId, StringComparison.Ordinal);
        set
        {
            if (value)
            {
                SelectPreset(LoopTuningPresetCatalog.ConservativeId);
            }
        }
    }

    public bool IsBalancedPresetSelected
    {
        get => string.Equals(SelectedPresetId, LoopTuningPresetCatalog.BalancedId, StringComparison.Ordinal);
        set
        {
            if (value)
            {
                SelectPreset(LoopTuningPresetCatalog.BalancedId);
            }
        }
    }

    public bool IsWildPresetSelected
    {
        get => string.Equals(SelectedPresetId, LoopTuningPresetCatalog.WildId, StringComparison.Ordinal);
        set
        {
            if (value)
            {
                SelectPreset(LoopTuningPresetCatalog.WildId);
            }
        }
    }

    public ICommand BackCommand { get; }

    public ICommand ResetBalancedCommand { get; }

    public ICommand RefreshCacheStatsCommand { get; }

    public ICommand OpenCacheFolderCommand { get; }

    public ICommand ClearCacheCommand { get; }

    private void SelectPreset(string presetId)
    {
        if (_isLoadingTuning)
        {
            return;
        }

        LoopTuningPresetDefinition preset = LoopTuningPresetCatalog.GetById(presetId);
        var settings = new LoopTuningSettings();
        LoopTuningPresetCatalog.ApplyPreset(settings, preset);
        LoadFromSettings(settings);
        MarkTuningDirty();
    }

    private void LoadFromSettings(LoopTuningSettings settings)
    {
        _isLoadingTuning = true;

        try
        {
            SelectedPresetId = LoopTuningPresetCatalog.GetById(settings.Preset).Id;
            _similarityThreshold = settings.SimilarityThreshold;
            _lookaheadDepth = settings.LookaheadDepth;
            _minJumpDistance = settings.MinJumpDistance;
            _maxBranchesPerBeat = settings.MaxBranchesPerBeat;
            _jumpProbability = settings.JumpProbability;
            _jumpCooldown = settings.JumpCooldown;
            _firstPassLinearPlaybackRatio = settings.FirstPassLinearPlaybackRatio;

            OnPropertyChanged(nameof(SimilarityThreshold));
            OnPropertyChanged(nameof(LookaheadDepth));
            OnPropertyChanged(nameof(MinJumpDistance));
            OnPropertyChanged(nameof(MaxBranchesPerBeat));
            OnPropertyChanged(nameof(JumpProbability));
            OnPropertyChanged(nameof(JumpCooldown));
            OnPropertyChanged(nameof(FirstPassLinearPlaybackRatio));
        }
        finally
        {
            _isLoadingTuning = false;
        }
    }

    private void MarkTuningDirty()
    {
        if (_isLoadingTuning)
        {
            return;
        }

        IsTuningDirty = true;
        TuningStatusText = "Applying tuning...";
        TuningGraphSummaryText = "No track loaded. Changes will apply to the next track.";
        ScheduleAutoApply();
    }

    private void ScheduleAutoApply()
    {
        _autoApplyCts?.Cancel();
        _autoApplyCts?.Dispose();
        _autoApplyCts = new CancellationTokenSource();

        _ = AutoApplyAsync(_autoApplyCts.Token);
    }

    private async Task AutoApplyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(450, cancellationToken).ConfigureAwait(true);
            await SaveTuningAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SaveTuningAsync(CancellationToken cancellationToken = default)
    {
        if (_isApplyingAutomatically)
        {
            return;
        }

        _isApplyingAutomatically = true;
        IsTuningBusy = true;

        try
        {
            CopyToUserSettings();
            await _settingsRepository
                .SaveAsync(_userSettings, cancellationToken)
                .ConfigureAwait(true);

            IsTuningDirty = false;
            TuningStatusText = "Tuning saved. It will be applied to the next track.";
            TuningGraphSummaryText = "No track loaded. Changes will apply to the next track.";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            TuningStatusText = $"Could not save tuning: {exception.Message}";
        }
        finally
        {
            IsTuningBusy = false;
            _isApplyingAutomatically = false;
        }
    }

    private async Task ResetBalancedAsync()
    {
        LoopTuningPresetDefinition preset = LoopTuningPresetCatalog.GetById(
            LoopTuningPresetCatalog.BalancedId);
        var settings = new LoopTuningSettings();
        LoopTuningPresetCatalog.ApplyPreset(settings, preset);
        LoadFromSettings(settings);
        IsTuningDirty = true;
        TuningStatusText = "Applying tuning...";
        await SaveTuningAsync().ConfigureAwait(true);
    }

    private async Task RefreshCacheStatsAsync()
    {
        IsCacheBusy = true;
        CachePathText = _pathProvider.CacheDirectory;

        try
        {
            AnalysisCacheStats stats = await _cacheService
                .GetStatsAsync()
                .ConfigureAwait(true);

            CacheSummaryText = $"{stats.FileCount} cached file(s), {FormatBytes(stats.TotalBytes)} used";
        }
        catch (Exception exception)
        {
            CacheSummaryText = $"Cache unavailable: {exception.Message}";
        }
        finally
        {
            IsCacheBusy = false;
        }
    }

    private Task OpenCacheFolderAsync()
    {
        _pathProvider.EnsureDirectories();

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = _pathProvider.CacheDirectory,
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }

    private async Task ClearCacheAsync()
    {
        MessageBoxResult result = MessageBox.Show(
            "Clear cached analyses and recent tracks? Songs will stay safe, but tracks will be analyzed again next time.",
            "Clear EternalLoop cache",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        IsCacheBusy = true;

        try
        {
            await _cacheService.ClearAsync().ConfigureAwait(true);
            await _recentTracksService.ClearAsync().ConfigureAwait(true);
            await RefreshCacheStatsAsync().ConfigureAwait(true);
            CacheSummaryText = "Cache is empty. Recent tracks were cleared.";
            TuningStatusText = "Cache and recent tracks cleared.";
        }
        finally
        {
            IsCacheBusy = false;
        }
    }

    private void CopyToUserSettings()
    {
        _userSettings.Tuning.Preset = SelectedPresetId;
        _userSettings.Tuning.SimilarityThreshold = SimilarityThreshold;
        _userSettings.Tuning.LookaheadDepth = LookaheadDepth;
        _userSettings.Tuning.MinJumpDistance = MinJumpDistance;
        _userSettings.Tuning.MaxBranchesPerBeat = MaxBranchesPerBeat;
        _userSettings.Tuning.JumpProbability = JumpProbability;
        _userSettings.Tuning.JumpCooldown = JumpCooldown;
        _userSettings.Tuning.FirstPassLinearPlaybackRatio = FirstPassLinearPlaybackRatio;
        _userSettings.Tuning.BranchQuantumType = "beats";
        _userSettings.Tuning.BranchMaxThreshold = LoopTuningPresetCatalog
            .GetById(SelectedPresetId)
            .BranchMaxThreshold;
        _userSettings.Tuning.AnalysisMusicalQuality = true;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{value:0.0} {units[unitIndex]}";
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
}
