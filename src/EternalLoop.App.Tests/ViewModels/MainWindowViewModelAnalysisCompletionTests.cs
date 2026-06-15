using EternalLoop.App.Services;
using EternalLoop.App.ViewModels;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using System.IO;
using System.Windows.Media;
using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class MainWindowViewModelAnalysisCompletionTests
{
    [Fact]
    public async Task AnalysisCompletionShouldLogAndReturnHomeWhenPlayerCreationFails()
    {
        using var paths = new TempPaths();
        string audioPath = Path.Combine(paths.Root, "track.wav");
        await File.WriteAllBytesAsync(audioPath, [0]);
        var logger = new RecordingLogger();
        var viewModel = new MainWindowViewModel(
            new FakeFilePickerService(),
            new FakeWorkflowService(),
            new EternalLoopUserSettings(),
            new FakeSettingsRepository(),
            new FakeAnalysisCacheService(),
            new FakePathProvider(paths.Root),
            recentTracksService: new FakeRecentTracksService(),
            trackArtworkService: new ThrowingArtworkService(),
            logger: logger);
        TrackWorkflowResult result = TrackWorkflowResult.Completed(
            TrackInput.FromFilePath(audioPath),
            new TrackAnalysisSummary(TimeSpan.FromSeconds(2), 2, 0, 0),
            new TrackBranchSummary(1, 1),
            PlayerViewModelDisposalTests.CreatePackage(),
            analysisSource: "Test analysis");

        Func<Task> complete = () => viewModel.CompleteAnalysisAsync(result);

        await complete.Should().NotThrowAsync();
        viewModel.CurrentViewModel.Should().BeOfType<WelcomeViewModel>();
        logger.Entries.Should().Contain(entry =>
            entry.Level == AppLogLevel.Error
            && entry.Message == "Analysis completion failed."
            && entry.Exception is InvalidOperationException);
    }

    private sealed class TempPaths : IDisposable
    {
        public TempPaths()
        {
            Root = Directory.CreateTempSubdirectory("eternalloop-main-window-").FullName;
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
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

    private sealed class ThrowingArtworkService : ITrackArtworkService
    {
        public string GetDisplayTitle(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        public ImageSource? TryLoadArtwork(string filePath)
        {
            throw new InvalidOperationException("Artwork failed.");
        }
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<LogEntry> Entries { get; } = [];

        public void Log(AppLogLevel level, string message, Exception? exception = null)
        {
            Entries.Add(new LogEntry(level, message, exception));
        }
    }

    private sealed record LogEntry(AppLogLevel Level, string Message, Exception? Exception);
}
