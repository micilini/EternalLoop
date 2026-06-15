using EternalLoop.Core.Cache;
using EternalLoop.Core.Recent;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;
using EternalLoop.Playback.Runtime;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Cache;

public sealed class TrackRuntimePackageCacheServiceTests
{
    [Fact]
    public async Task TryLoadAsyncShouldMissWhenIndexDoesNotExist()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);

        TrackRuntimePackageCacheResult result = await service.TryLoadAsync(
            TrackInput.FromFilePath(audioPath),
            identity,
            LoopTuningSettings.Balanced(),
            4);

        result.IsHit.Should().BeFalse();
    }

    [Fact]
    public async Task TryLoadAsyncShouldHitWhenFileTuningAndSchemaMatch()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        TrackRuntimePackage package = CreatePackage(identity, paths);
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();

        RuntimePackageCacheIndexItem item = await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4, package);
        TrackRuntimePackageCacheResult result = await service.TryLoadAsync(
            TrackInput.FromFilePath(audioPath),
            identity,
            tuning,
            4);

        item.CacheKey.Should().Be(item.RuntimeCacheKey);
        item.AnalysisCacheKey.Should().NotBeNullOrWhiteSpace();
        item.BranchCacheKey.Should().NotBeNullOrWhiteSpace();
        item.RuntimeCacheKey.Should().NotBeNullOrWhiteSpace();
        result.IsHit.Should().BeTrue();
        result.RuntimePackage.Should().NotBeNull();
        result.RuntimePackage!.RuntimeTrack.Beats.Should().ContainSingle();
        result.RuntimePackage.Files.AudioPath.Should().Be(identity.FilePath);
    }

    [Fact]
    public async Task TryLoadAsyncShouldMissWhenManifestDoesNotExist()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4, CreatePackage(identity, paths));
        File.Delete(Path.Combine(paths.Provider.WorkflowCacheDirectory, "run", "runtime-package.json"));

        TrackRuntimePackageCacheResult result = await service.TryLoadAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4);

        result.IsHit.Should().BeFalse();
    }

    [Fact]
    public async Task TryLoadAsyncShouldMissWhenAudioDoesNotExist()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4, CreatePackage(identity, paths));
        File.Delete(audioPath);

        TrackRuntimePackageCacheResult result = await service.TryLoadAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4);

        result.IsHit.Should().BeFalse();
    }

    [Fact]
    public async Task TryLoadAsyncShouldMissWhenTuningFingerprintChanges()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4, CreatePackage(identity, paths));
        LoopTuningSettings changed = LoopTuningSettings.Balanced();
        changed.JumpProbability = 0.99;

        TrackRuntimePackageCacheResult result = await service.TryLoadAsync(TrackInput.FromFilePath(audioPath), identity, changed, 4);

        result.IsHit.Should().BeFalse();
    }

    [Fact]
    public async Task TryLoadCompatibleBranchAsyncShouldRebuildRuntimeTuning()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4, CreatePackage(identity, paths));

        LoopTuningSettings changed = LoopTuningSettings.Balanced();
        changed.JumpProbability = 0.91;
        changed.JumpCooldown = 2;
        changed.FirstPassLinearPlaybackRatio = 0.1;

        TrackRuntimePackageCacheResult exact = await service.TryLoadExactRuntimeAsync(
            TrackInput.FromFilePath(audioPath),
            identity,
            changed,
            4);
        TrackRuntimePackageCacheResult compatible = await service.TryLoadCompatibleBranchAsync(
            TrackInput.FromFilePath(audioPath),
            identity,
            changed,
            4);

        exact.IsHit.Should().BeFalse();
        compatible.IsHit.Should().BeTrue();
        compatible.RuntimePackage.Should().NotBeNull();
        compatible.RuntimePackage!.BranchDecisionOptions.JumpProbability.Should().Be(0.91);
        compatible.RuntimePackage.BranchDecisionOptions.JumpCooldownBeats.Should().Be(2);
        compatible.RuntimePackage.BranchDecisionOptions.FirstPassLinearPlaybackRatio.Should().Be(0.1);
        compatible.RuntimePackage.Tuning.JumpProbability.Should().Be(0.91);
    }

    [Fact]
    public async Task TryLoadCompatibleBranchAsyncShouldMissWhenBranchTuningChanges()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4, CreatePackage(identity, paths));

        LoopTuningSettings changed = LoopTuningSettings.Balanced();
        changed.SimilarityThreshold = 0.8;

        TrackRuntimePackageCacheResult compatible = await service.TryLoadCompatibleBranchAsync(
            TrackInput.FromFilePath(audioPath),
            identity,
            changed,
            4);

        compatible.IsHit.Should().BeFalse();
    }

    [Fact]
    public async Task TryLoadAsyncShouldMissWhenCacheIndexIsCorrupt()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        await File.WriteAllTextAsync(paths.Provider.RuntimeCacheIndexFilePath, "{bad json");
        var service = new TrackRuntimePackageCacheService(paths.Provider);

        TrackRuntimePackageCacheResult result = await service.TryLoadAsync(
            TrackInput.FromFilePath(audioPath),
            identity,
            LoopTuningSettings.Balanced(),
            4);

        result.IsHit.Should().BeFalse();
        Directory.GetFiles(paths.Provider.WorkflowCacheDirectory, "cache-index.json.corrupt-*.bak").Should().ContainSingle();
    }

    [Fact]
    public async Task TryLoadAsyncShouldMissWhenRuntimeManifestIsCorrupt()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4, CreatePackage(identity, paths));
        string manifestPath = Path.Combine(paths.Provider.WorkflowCacheDirectory, "run", "runtime-package.json");
        await File.WriteAllTextAsync(manifestPath, "{bad json");

        TrackRuntimePackageCacheResult result = await service.TryLoadAsync(TrackInput.FromFilePath(audioPath), identity, tuning, 4);

        result.IsHit.Should().BeFalse();
        Directory.GetFiles(Path.GetDirectoryName(manifestPath)!, "runtime-package.json.corrupt-*.bak").Should().ContainSingle();
    }

    [Fact]
    public async Task ClearCacheShouldPreserveRecentTracksFile()
    {
        using TempCachePaths paths = TempCachePaths.Create();
        paths.Provider.EnsureDirectories();
        await File.WriteAllTextAsync(paths.Provider.RecentTracksFilePath, "{\"schemaVersion\":1,\"items\":[]}");
        string audioPath = paths.CreateAudio();
        TrackFileIdentity identity = await new TrackFileIdentityService().CreateAsync(audioPath);
        var service = new TrackRuntimePackageCacheService(paths.Provider);
        await service.SaveAsync(TrackInput.FromFilePath(audioPath), identity, LoopTuningSettings.Balanced(), 4, CreatePackage(identity, paths));

        await new AnalysisCacheService(paths.Provider).ClearAsync();

        File.Exists(paths.Provider.RecentTracksFilePath).Should().BeTrue();
        File.Exists(paths.Provider.RuntimeCacheIndexFilePath).Should().BeFalse();
    }

    private static TrackRuntimePackage CreatePackage(TrackFileIdentity identity, TempCachePaths paths)
    {
        string runRoot = Path.Combine(paths.Provider.WorkflowCacheDirectory, "run");
        var runtimeTrack = new TrackRuntimeBuilder().Build(new TrackRuntimeBuildRequest
        {
            Id = "track",
            Title = "Track",
            Artist = "Local",
            AudioPath = identity.FilePath,
            DurationSeconds = 1,
            Beats = [new RuntimeBeatInput(0, 0, 1, 1)]
        }).Track;

        return new TrackRuntimePackage(
            new TrackRuntimeMetadata("track", "Track", "Local", identity.Sha256, 1, 120, 4, "test", 4, DateTime.UtcNow),
            new TrackRuntimeFileSet(runRoot, identity.FilePath, Path.Combine(runRoot, "analysis.json"), Path.Combine(runRoot, "branches.json")),
            new TrackRuntimeTuningSnapshot("Balanced", 0.86, 1, 4, 4, "beats", 80, true, 0.22, 12, 0.78),
            runtimeTrack,
            new BranchDecisionOptions(),
            new TrackRuntimePreparationSummary(1, 0, true),
            0,
            0);
    }

    private sealed class TempCachePaths : IDisposable
    {
        private TempCachePaths(string root)
        {
            Root = root;
            Provider = new AppPathProvider(root);
            Provider.EnsureDirectories();
        }

        public string Root { get; }

        public AppPathProvider Provider { get; }

        public static TempCachePaths Create()
        {
            return new TempCachePaths(Directory.CreateTempSubdirectory("eternalloop-cache-").FullName);
        }

        public string CreateAudio()
        {
            string path = Path.Combine(Root, "track.mp3");
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
