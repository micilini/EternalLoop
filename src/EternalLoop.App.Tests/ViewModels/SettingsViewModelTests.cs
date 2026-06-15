using EternalLoop.App.Tests.TestDoubles;
using EternalLoop.App.ViewModels;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Settings;
using FluentAssertions;
using System.IO;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task ResetBalancedCommandShouldRestoreBalancedPresetValues()
    {
        var settings = new EternalLoopUserSettings { Tuning = LoopTuningSettings.Balanced() };
        LoopTuningPresetCatalog.ApplyPreset(settings.Tuning, LoopTuningPresetCatalog.GetById(LoopTuningPresetCatalog.WildId));
        var repository = new FakeSettingsRepository();
        var viewModel = CreateViewModel(settings, repository: repository);

        viewModel.ResetBalancedCommand.Execute(null);

        await AsyncTest.EventuallyAsync(() =>
        {
            viewModel.SelectedPresetId.Should().Be(LoopTuningPresetCatalog.BalancedId);
            viewModel.JumpProbability.Should().Be(0.85);
            viewModel.JumpCooldown.Should().Be(4);
            viewModel.FirstPassLinearPlaybackRatio.Should().Be(0.10);
            viewModel.MaxBranchesPerBeat.Should().Be(6);
            viewModel.TuningStatusText.Should().Be("Tuning saved. It will be applied to the next track.");
            repository.SaveCount.Should().Be(1);
        });
    }

    [Fact]
    public async Task RefreshCacheStatsCommandShouldUpdateCacheSummaryText()
    {
        var cache = new FakeAnalysisCacheService(new AnalysisCacheStats(3, 2048));
        var viewModel = CreateViewModel(cacheService: cache);

        viewModel.RefreshCacheStatsCommand.Execute(null);

        await AsyncTest.EventuallyAsync(() =>
        {
            viewModel.CacheSummaryText.Should().StartWith("3 cached file(s), ");
            viewModel.CacheSummaryText.Should().EndWith(" KB used");
        });
    }

    [Fact]
    public async Task SaveFailureShouldShowFriendlyTuningStatus()
    {
        var repository = new FakeSettingsRepository(new IOException("disk full"));
        var viewModel = CreateViewModel(repository: repository);

        viewModel.ResetBalancedCommand.Execute(null);

        await AsyncTest.EventuallyAsync(() =>
            viewModel.TuningStatusText.Should().Be("Could not save tuning: disk full"));
    }

    [Fact]
    public void BackCommandShouldInvokeBackCallback()
    {
        bool backInvoked = false;
        var viewModel = CreateViewModel(back: () => backInvoked = true);

        viewModel.BackCommand.Execute(null);

        backInvoked.Should().BeTrue();
    }

    [Fact]
    public void CachePathTextShouldBeInitializedFromPathProvider()
    {
        var paths = new FakePathProvider("C:\\temp\\eternalloop-test");
        var viewModel = CreateViewModel(pathProvider: paths);

        viewModel.CachePathText.Should().Be(paths.CacheDirectory);
    }

    [Fact]
    public async Task ChangingTuningShouldScheduleAndPersistSettings()
    {
        var settings = new EternalLoopUserSettings { Tuning = LoopTuningSettings.Balanced() };
        var repository = new FakeSettingsRepository();
        var viewModel = CreateViewModel(settings, repository: repository);

        viewModel.JumpProbability = 0.33;

        await AsyncTest.EventuallyAsync(() =>
        {
            repository.SaveCount.Should().Be(1);
            settings.Tuning.JumpProbability.Should().Be(0.33);
            viewModel.IsTuningDirty.Should().BeFalse();
        });
    }

    private static SettingsViewModel CreateViewModel(
        EternalLoopUserSettings? settings = null,
        FakeSettingsRepository? repository = null,
        FakeAnalysisCacheService? cacheService = null,
        FakePathProvider? pathProvider = null,
        Action? back = null)
    {
        return new SettingsViewModel(
            back ?? (() => { }),
            settings ?? new EternalLoopUserSettings { Tuning = LoopTuningSettings.Balanced() },
            repository ?? new FakeSettingsRepository(),
            cacheService ?? new FakeAnalysisCacheService(new AnalysisCacheStats(0, 0)),
            pathProvider ?? new FakePathProvider(Path.Combine(Path.GetTempPath(), "eternalloop-settings-tests")),
            new FakeRecentTracksService());
    }

    private sealed class FakeSettingsRepository(Exception? saveException = null) : IUserSettingsRepository
    {
        public int SaveCount { get; private set; }

        public Task<EternalLoopUserSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EternalLoopUserSettings { Tuning = LoopTuningSettings.Balanced() });
        }

        public Task SaveAsync(EternalLoopUserSettings settings, CancellationToken cancellationToken = default)
        {
            SaveCount++;

            if (saveException is not null)
            {
                throw saveException;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAnalysisCacheService(AnalysisCacheStats stats) : IAnalysisCacheService
    {
        public Task<AnalysisCacheStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(stats);
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
}
