using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EternalLoop.App.Commands;
using EternalLoop.App.Services;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Visualization;

namespace EternalLoop.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IFilePickerService _filePickerService;
    private readonly ITrackWorkflowService _workflowService;
    private readonly EternalLoopUserSettings _userSettings;
    private readonly IUserSettingsRepository _settingsRepository;
    private readonly IAnalysisCacheService _cacheService;
    private readonly IAppPathProvider _pathProvider;
    private readonly TrackFileIdentityService _fileIdentityService;
    private readonly IRecentTracksService _recentTracksService;
    private readonly ITrackArtworkService _trackArtworkService;
    private readonly IAudioLoader _audioLoader;
    private readonly ILoopingAudioPlayerFactory _playerFactory;
    private readonly BranchGraphBuilder _graphBuilder;
    private readonly IAppLogger _logger;
    private object? _currentViewModel;
    private bool _disposed;

    public MainWindowViewModel(
        IFilePickerService filePickerService,
        ITrackWorkflowService workflowService,
        EternalLoopUserSettings userSettings,
        IUserSettingsRepository settingsRepository,
        IAnalysisCacheService cacheService,
        IAppPathProvider pathProvider,
        TrackFileIdentityService? fileIdentityService = null,
        IRecentTracksService? recentTracksService = null,
        ITrackArtworkService? trackArtworkService = null,
        IAudioLoader? audioLoader = null,
        ILoopingAudioPlayerFactory? playerFactory = null,
        BranchGraphBuilder? graphBuilder = null,
        IAppLogger? logger = null)
    {
        _filePickerService = filePickerService
            ?? throw new ArgumentNullException(nameof(filePickerService));
        _workflowService = workflowService
            ?? throw new ArgumentNullException(nameof(workflowService));
        _userSettings = userSettings
            ?? throw new ArgumentNullException(nameof(userSettings));
        _settingsRepository = settingsRepository
            ?? throw new ArgumentNullException(nameof(settingsRepository));
        _cacheService = cacheService
            ?? throw new ArgumentNullException(nameof(cacheService));
        _pathProvider = pathProvider
            ?? throw new ArgumentNullException(nameof(pathProvider));
        _logger = logger ?? NullAppLogger.Instance;
        _fileIdentityService = fileIdentityService ?? new TrackFileIdentityService();
        _recentTracksService = recentTracksService ?? new RecentTracksService(new JsonRecentTracksRepository(pathProvider, _logger));
        _trackArtworkService = trackArtworkService ?? new TrackArtworkService();
        _audioLoader = audioLoader ?? new AudioLoader();
        _playerFactory = playerFactory ?? new LoopingAudioPlayerFactory(new DefaultBranchRandomProvider());
        _graphBuilder = graphBuilder ?? new BranchGraphBuilder();

        NavigateHomeCommand = new RelayCommand(NavigateHome);
        OpenAnotherTrackCommand = new RelayCommand(OpenAnotherTrack);
        NavigateRecentTracksCommand = new RelayCommand(NavigateRecentTracks);
        NavigateSettingsCommand = new RelayCommand(NavigateSettings);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand NavigateHomeCommand { get; }

    public ICommand OpenAnotherTrackCommand { get; }

    public ICommand NavigateRecentTracksCommand { get; }

    public ICommand NavigateSettingsCommand { get; }

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public void Initialize()
    {
        NavigateHome();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_currentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _currentViewModel = null;
    }

    private void NavigateHome()
    {
        SetCurrentViewModel(new WelcomeViewModel(
            _filePickerService,
            filePath => StartAnalysis(filePath, forceReanalysis: false)));
    }

    private void OpenAnotherTrack()
    {
        string? filePath = _filePickerService.PickAudioFile();

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            StartAnalysis(filePath, forceReanalysis: false);
        }
    }

    private void NavigateRecentTracks()
    {
        SetCurrentViewModel(new RecentTracksViewModel(
            _recentTracksService,
            filePath => StartAnalysis(filePath, forceReanalysis: false),
            NavigateHome));
    }

    private void NavigateSettings()
    {
        SetCurrentViewModel(new SettingsViewModel(
            NavigateHome,
            _userSettings,
            _settingsRepository,
            _cacheService,
            _pathProvider,
            _recentTracksService));
    }

    private void StartAnalysis(string filePath, bool forceReanalysis = false)
    {
        SetCurrentViewModel(new AnalysisViewModel(
            filePath,
            _workflowService,
            NavigateHome,
            OnAnalysisCompleted,
            forceReanalysis));
    }

    private async void OnAnalysisCompleted(TrackWorkflowResult result)
    {
        try
        {
            await CompleteAnalysisAsync(result).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _logger.Log(AppLogLevel.Error, "Analysis completion failed.", exception);

            if (!_disposed)
            {
                NavigateHome();
            }
        }
    }

    internal async Task CompleteAnalysisAsync(TrackWorkflowResult result)
    {
        if (_disposed)
        {
            return;
        }

        if (!result.IsSuccess || result.RuntimePackage is null)
        {
            return;
        }

        try
        {
            await RegisterRecentTrackAsync(result).ConfigureAwait(true);

            if (_disposed)
            {
                return;
            }

            SetCurrentViewModel(CreatePlayerViewModel(result.RuntimePackage, result.AnalysisSource));
        }
        catch (Exception exception)
        {
            _logger.Log(AppLogLevel.Error, "Analysis completion failed.", exception);

            if (!_disposed)
            {
                NavigateHome();
            }
        }
    }

    private async Task RegisterRecentTrackAsync(TrackWorkflowResult result)
    {
        if (result.RuntimePackage is null)
        {
            return;
        }

        try
        {
            TrackFileIdentity identity = await _fileIdentityService
                .CreateAsync(result.Input.FilePath)
                .ConfigureAwait(true);
            string manifestPath = Path.Combine(result.RuntimePackage.Files.RunRoot, "runtime-package.json");

            await _recentTracksService
                .AddOrUpdateAsync(new RecentTracksUpdateRequest(
                    identity,
                    result.RuntimePackage,
                    manifestPath,
                    result.RuntimePackage.Files.RunRoot,
                    DateTime.UtcNow))
                .ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _logger.Log(AppLogLevel.Warning, "Recent track registration failed.", exception);
        }
    }

    private PlayerViewModel CreatePlayerViewModel(
        TrackRuntimePackage runtimePackage,
        string analysisSource)
    {
        return new PlayerViewModel(
            runtimePackage,
            StartAnalysis,
            NavigateHome,
            _trackArtworkService,
            _audioLoader,
            _playerFactory,
            _graphBuilder,
            analysisSource,
            _logger);
    }

    private void SetCurrentViewModel(object? viewModel)
    {
        if (_disposed)
        {
            if (viewModel is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }

            return;
        }

        if (ReferenceEquals(_currentViewModel, viewModel))
        {
            return;
        }

        object? previousViewModel = _currentViewModel;
        CurrentViewModel = null;

        if (previousViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        CurrentViewModel = viewModel;
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
