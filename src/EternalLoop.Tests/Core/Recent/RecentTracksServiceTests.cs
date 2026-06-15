using System.Text.Json;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using EternalLoop.Playback.Runtime;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Recent;

public sealed class RecentTracksServiceTests
{
    [Fact]
    public async Task AddOrUpdateAsyncShouldSaveNewItem()
    {
        using TempRecentPaths paths = TempRecentPaths.Create();
        RecentTracksService service = CreateService(paths);
        string audioPath = paths.CreateAudio("one.mp3");
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);

        IReadOnlyList<RecentTrackEntry> items = await service.AddOrUpdateAsync(CreateRequest(identity, paths));

        items.Should().ContainSingle();
        items[0].FilePath.Should().Be(identity.FilePath);
        File.Exists(paths.Provider.RecentTracksFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task AddOrUpdateAsyncShouldDeduplicateByNormalizedPathAndMoveToTop()
    {
        using TempRecentPaths paths = TempRecentPaths.Create();
        RecentTracksService service = CreateService(paths);
        TrackFileIdentity first = await new TrackFileIdentityService().CreateAsync(paths.CreateAudio("one.mp3"));
        TrackFileIdentity second = await new TrackFileIdentityService().CreateAsync(paths.CreateAudio("two.mp3"));

        await service.AddOrUpdateAsync(CreateRequest(first, paths, DateTime.UtcNow.AddMinutes(-2)));
        await service.AddOrUpdateAsync(CreateRequest(second, paths, DateTime.UtcNow.AddMinutes(-1)));
        IReadOnlyList<RecentTrackEntry> items = await service.AddOrUpdateAsync(CreateRequest(first, paths, DateTime.UtcNow));

        items.Should().HaveCount(2);
        items[0].FilePath.Should().Be(first.FilePath);
    }

    [Fact]
    public async Task AddOrUpdateAsyncShouldKeepMaximumTenItems()
    {
        using TempRecentPaths paths = TempRecentPaths.Create();
        RecentTracksService service = CreateService(paths);

        for (int index = 0; index < 12; index++)
        {
            TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(paths.CreateAudio($"{index}.mp3"));
            await service.AddOrUpdateAsync(CreateRequest(identity, paths, DateTime.UtcNow.AddMinutes(index)));
        }

        IReadOnlyList<RecentTrackEntry> items = await service.LoadAsync();
        items.Should().HaveCount(10);
    }

    [Fact]
    public async Task RemoveMissingAsyncShouldRemoveMissingFiles()
    {
        using TempRecentPaths paths = TempRecentPaths.Create();
        RecentTracksService service = CreateService(paths);
        string audioPath = paths.CreateAudio("missing.mp3");
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        await service.AddOrUpdateAsync(CreateRequest(identity, paths));
        File.Delete(audioPath);

        IReadOnlyList<RecentTrackEntry> items = await service.RemoveMissingAsync();

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsyncShouldRemoveAllRecentTracks()
    {
        using TempRecentPaths paths = TempRecentPaths.Create();
        RecentTracksService service = CreateService(paths);
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(paths.CreateAudio("one.mp3"));
        await service.AddOrUpdateAsync(CreateRequest(identity, paths));

        await service.ClearAsync();

        IReadOnlyList<RecentTrackEntry> items = await service.LoadAsync();
        items.Should().BeEmpty();
        File.Exists(paths.Provider.RecentTracksFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsyncShouldReturnEmptyForCorruptJson()
    {
        using TempRecentPaths paths = TempRecentPaths.Create();
        paths.Provider.EnsureDirectories();
        await File.WriteAllTextAsync(paths.Provider.RecentTracksFilePath, "{bad json");
        RecentTracksService service = CreateService(paths);

        IReadOnlyList<RecentTrackEntry> items = await service.LoadAsync();

        items.Should().BeEmpty();
        Directory.GetFiles(paths.Root, "recent-tracks.json.corrupt-*.bak").Should().ContainSingle();
    }

    private static RecentTracksService CreateService(TempRecentPaths paths)
    {
        return new RecentTracksService(new JsonRecentTracksRepository(paths.Provider));
    }

    private static RecentTracksUpdateRequest CreateRequest(
        TrackFileIdentity identity,
        TempRecentPaths paths,
        DateTime? time = null)
    {
        TrackRuntimePackage package = CreatePackage(identity, paths);
        return new RecentTracksUpdateRequest(
            identity,
            package,
            Path.Combine(paths.Provider.WorkflowCacheDirectory, "run", "runtime-package.json"),
            package.Files.RunRoot,
            time ?? DateTime.UtcNow);
    }

    private static TrackRuntimePackage CreatePackage(TrackFileIdentity identity, TempRecentPaths paths)
    {
        var runtimeTrack = new TrackRuntimeBuilder().Build(new TrackRuntimeBuildRequest
        {
            Id = "track",
            Title = identity.FileName,
            Artist = "Local",
            AudioPath = identity.FilePath,
            DurationSeconds = 1,
            Beats = [new RuntimeBeatInput(0, 0, 1, 1)]
        }).Track;

        return new TrackRuntimePackage(
            new TrackRuntimeMetadata("track", identity.FileName, "Local", identity.Sha256, 1, 120, 4, "test", 4, DateTime.UtcNow),
            new TrackRuntimeFileSet(Path.Combine(paths.Provider.WorkflowCacheDirectory, "run"), identity.FilePath, "analysis.json", "branches.json"),
            new TrackRuntimeTuningSnapshot("Balanced", 0.86, 1, 4, 4, "beats", 80, true, 0.22, 12, 0.78),
            runtimeTrack,
            new BranchDecisionOptions(),
            new TrackRuntimePreparationSummary(1, 0, true),
            0,
            0);
    }

    private sealed class TempRecentPaths : IDisposable
    {
        private TempRecentPaths(string root)
        {
            Root = root;
            Provider = new AppPathProvider(root);
            Provider.EnsureDirectories();
        }

        public string Root { get; }

        public AppPathProvider Provider { get; }

        public static TempRecentPaths Create()
        {
            return new TempRecentPaths(Directory.CreateTempSubdirectory("eternalloop-recent-").FullName);
        }

        public string CreateAudio(string fileName)
        {
            string path = Path.Combine(Root, fileName);
            File.WriteAllBytes(path, [1, 2, 3, 4]);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
