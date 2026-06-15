using EternalLoop.App.Services;
using EternalLoop.App.ViewModels;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using FluentAssertions;
using System.IO;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class MainWindowViewModelNavigationTests
{
    [Fact]
    public void InitializeShouldStartAtWelcomeViewModel()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        viewModel.Initialize();

        viewModel.CurrentViewModel.Should().BeOfType<WelcomeViewModel>();
    }

    [Fact]
    public void NavigateSettingsCommandShouldSwitchToSettingsViewModel()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Initialize();

        viewModel.NavigateSettingsCommand.Execute(null);

        viewModel.CurrentViewModel.Should().BeOfType<SettingsViewModel>();
    }

    [Fact]
    public void NavigateRecentTracksCommandShouldSwitchToRecentTracksViewModel()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        viewModel.Initialize();

        viewModel.NavigateRecentTracksCommand.Execute(null);

        viewModel.CurrentViewModel.Should().BeOfType<RecentTracksViewModel>();
    }

    [Fact]
    public void OpenAnotherTrackCommandShouldStartAnalysisWhenPickerReturnsFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "track.wav");
        MainWindowViewModel viewModel = CreateViewModel(filePicker: new FakeFilePickerService(path));
        viewModel.Initialize();

        viewModel.OpenAnotherTrackCommand.Execute(null);

        viewModel.CurrentViewModel.Should().BeOfType<AnalysisViewModel>();
    }

    [Fact]
    public async Task SuccessfulAnalysisCompletionShouldSwitchToPlayerViewModel()
    {
        using var temp = new TempAudioFile();
        MainWindowViewModel viewModel = CreateViewModel();
        TrackWorkflowResult result = TrackWorkflowResult.Completed(
            TrackInput.FromFilePath(temp.Path),
            new TrackAnalysisSummary(TimeSpan.FromSeconds(2), 2, 0, 0),
            new TrackBranchSummary(1, 1),
            PlayerViewModelDisposalTests.CreatePackage());

        await viewModel.CompleteAnalysisAsync(result);

        viewModel.CurrentViewModel.Should().BeOfType<PlayerViewModel>();
    }

    private static MainWindowViewModel CreateViewModel(IFilePickerService? filePicker = null)
    {
        string root = Path.Combine(Path.GetTempPath(), "eternalloop-main-window-tests", Guid.NewGuid().ToString("N"));

        return new MainWindowViewModel(
            filePicker ?? new FakeFilePickerService(null),
            new FakeWorkflowService(),
            new EternalLoopUserSettings { Tuning = LoopTuningSettings.Balanced() },
            new FakeSettingsRepository(),
            new FakeAnalysisCacheService(),
            new FakePathProvider(root),
            recentTracksService: new FakeRecentTracksService(),
            trackArtworkService: new PlayerViewModelDisposalTests.FakeArtworkService(null),
            audioLoader: new PlayerViewModelDisposalTests.FakeAudioLoader(),
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer()),
            logger: new FakeLogger());
    }

    private sealed class TempAudioFile : IDisposable
    {
        public TempAudioFile()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
            File.WriteAllBytes(Path, [0]);
        }

        public string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }

    private sealed class FakeFilePickerService(string? path) : IFilePickerService
    {
        public string? PickAudioFile()
        {
            return path;
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
            return Task.FromResult(new EternalLoopUserSettings { Tuning = LoopTuningSettings.Balanced() });
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
