using System.Windows;
using EternalLoop.App.Services;
using EternalLoop.App.ViewModels;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;

namespace EternalLoop.App;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var pathProvider = new AppPathProvider();
        pathProvider.EnsureDirectories();
        IAppLogger logger = App.Logger;

        var settingsRepository = new JsonUserSettingsRepository(pathProvider, logger);
        EternalLoopUserSettings userSettings = settingsRepository
            .LoadAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        var cacheService = new AnalysisCacheService(pathProvider);
        var fileIdentityService = new TrackFileIdentityService();
        var runtimeCacheService = new TrackRuntimePackageCacheService(pathProvider, logger);
        var recentTracksService = new RecentTracksService(new JsonRecentTracksRepository(pathProvider, logger));
        var workflowOptions = new TrackWorkflowServiceOptions
        {
            WorkspaceRoot = pathProvider.WorkflowCacheDirectory,
            ForceIntermediateExports = true,
            PrettyIntermediateExports = true,
            Tuning = userSettings.Tuning,
            SettingsSchemaVersion = userSettings.SettingsSchemaVersion,
            FileIdentityService = fileIdentityService,
            RuntimePackageCacheService = runtimeCacheService,
            Logger = logger
        };

        _viewModel = new MainWindowViewModel(
            new FilePickerService(),
            TrackWorkflowServiceFactory.CreateDefault(workflowOptions),
            userSettings,
            settingsRepository,
            cacheService,
            pathProvider,
            fileIdentityService,
            recentTracksService,
            logger: logger);

        DataContext = _viewModel;
        _viewModel.Initialize();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel?.Dispose();
        _viewModel = null;
        base.OnClosed(e);
    }
}
