using System.IO;
using System.Reflection;
using EternalLoop.App.Services;
using EternalLoop.App.ViewModels;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using EternalLoop.Playback.Visualization;
using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class MainWindowViewModelDisposalTests
{
    [Fact]
    public void NavigateHome_disposes_previous_player_view_model()
    {
        var viewModel = CreateMainWindowViewModel();
        var playerViewModel = PlayerViewModelDisposalTests.CreateViewModel(
            artworkService: new PlayerViewModelDisposalTests.FakeArtworkService(new System.Windows.Media.DrawingImage()));

        SetCurrentViewModel(viewModel, playerViewModel);

        viewModel.NavigateHomeCommand.Execute(null);

        playerViewModel.Graph.Should().BeSameAs(BranchGraph.Empty);
        playerViewModel.TrackArtwork.Should().BeNull();
        viewModel.CurrentViewModel.Should().BeOfType<WelcomeViewModel>();
    }

    private static MainWindowViewModel CreateMainWindowViewModel()
    {
        string root = Path.Combine(Path.GetTempPath(), "eternalloop-app-tests", Guid.NewGuid().ToString("N"));

        return new MainWindowViewModel(
            new FakeFilePickerService(),
            new FakeWorkflowService(),
            new EternalLoopUserSettings(),
            new FakeSettingsRepository(),
            new FakeAnalysisCacheService(),
            new FakePathProvider(root),
            recentTracksService: new FakeRecentTracksService(),
            trackArtworkService: new PlayerViewModelDisposalTests.FakeArtworkService(null),
            logger: new FakeLogger());
    }

    private static void SetCurrentViewModel(MainWindowViewModel target, object value)
    {
        typeof(MainWindowViewModel)
            .GetMethod("SetCurrentViewModel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(target, [value]);
    }

    private sealed class FakeFilePickerService : IFilePickerService
    {
        public string? PickAudioFile()
        {
            return null;
        }
    }

    private sealed class FakeWorkflowService : ITrackWorkflowService
    {
        public Task<TrackWorkflowResult> RunAsync(
            TrackWorkflowRequest request,
            ITrackWorkflowProgressReporter? progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeSettingsRepository : IUserSettingsRepository
    {
        public Task<EternalLoopUserSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EternalLoopUserSettings());
        }

        public Task SaveAsync(EternalLoopUserSettings settings, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAnalysisCacheService : IAnalysisCacheService
    {
        public Task<AnalysisCacheStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AnalysisCacheStats(0, 0));
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePathProvider(string root) : IAppPathProvider
    {
        public string AppDataDirectory => root;

        public string CacheDirectory => Path.Combine(root, "cache");

        public string WorkflowCacheDirectory => Path.Combine(root, "workflow");

        public string LogsDirectory => Path.Combine(root, "logs");

        public string SettingsFilePath => Path.Combine(root, "settings.json");

        public string RecentTracksFilePath => Path.Combine(root, "recent.json");

        public string RuntimeCacheIndexFilePath => Path.Combine(root, "runtime-index.json");

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(AppDataDirectory);
        }
    }

    private sealed class FakeRecentTracksService : IRecentTracksService
    {
        public Task<IReadOnlyList<RecentTrackEntry>> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecentTrackEntry>>([]);
        }

        public Task<IReadOnlyList<RecentTrackEntry>> AddOrUpdateAsync(
            RecentTracksUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecentTrackEntry>>([]);
        }

        public Task<IReadOnlyList<RecentTrackEntry>> RemoveMissingAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecentTrackEntry>>([]);
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLogger : IAppLogger
    {
        public void Log(AppLogLevel level, string message, Exception? exception = null)
        {
        }
    }
}
