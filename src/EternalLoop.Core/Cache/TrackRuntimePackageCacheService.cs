using System.Text.Json;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;

namespace EternalLoop.Core.Cache;

public sealed class TrackRuntimePackageCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IAppPathProvider _pathProvider;
    private readonly TrackRuntimePackageManifestRepository _manifestRepository;
    private readonly IAppLogger _logger;

    public TrackRuntimePackageCacheService(IAppPathProvider pathProvider, IAppLogger? logger = null)
        : this(pathProvider, new TrackRuntimePackageManifestRepository(), logger)
    {
    }

    public TrackRuntimePackageCacheService(
        IAppPathProvider pathProvider,
        TrackRuntimePackageManifestRepository manifestRepository,
        IAppLogger? logger = null)
    {
        _pathProvider = pathProvider
            ?? throw new ArgumentNullException(nameof(pathProvider));
        _manifestRepository = manifestRepository
            ?? throw new ArgumentNullException(nameof(manifestRepository));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<TrackRuntimePackageCacheResult> TryLoadAsync(
        TrackInput input,
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion,
        CancellationToken cancellationToken = default)
    {
        return await TryLoadExactRuntimeAsync(
                input,
                identity,
                tuning,
                settingsSchemaVersion,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TrackRuntimePackageCacheResult> TryLoadExactRuntimeAsync(
        TrackInput input,
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tuning);

        _pathProvider.EnsureDirectories();
        string runtimeCacheKey = RuntimePackageCacheKey.CreateRuntimeKey(identity, tuning, settingsSchemaVersion);
        RuntimePackageCacheIndex index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        RuntimePackageCacheIndexItem? item = index.Items.FirstOrDefault(candidate =>
            IsRuntimeKeyMatch(candidate, runtimeCacheKey));

        if (!IsValidItem(item, identity))
        {
            return TrackRuntimePackageCacheResult.Miss;
        }

        return await TryLoadItemAsync(index, item!, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TrackRuntimePackageCacheResult> TryLoadCompatibleBranchAsync(
        TrackInput input,
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tuning);

        _pathProvider.EnsureDirectories();
        string branchCacheKey = RuntimePackageCacheKey.CreateBranchKey(identity, tuning, settingsSchemaVersion);
        string runtimeCacheKey = RuntimePackageCacheKey.CreateRuntimeKey(identity, tuning, settingsSchemaVersion);
        RuntimePackageCacheIndex index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);

        RuntimePackageCacheIndexItem? item = index.Items.FirstOrDefault(candidate =>
            IsSameFile(candidate, identity)
            && string.Equals(candidate.BranchCacheKey, branchCacheKey, StringComparison.OrdinalIgnoreCase)
            && !IsRuntimeKeyMatch(candidate, runtimeCacheKey));

        if (!IsValidItem(item, identity))
        {
            return TrackRuntimePackageCacheResult.Miss;
        }

        TrackRuntimePackageCacheResult loaded = await TryLoadItemAsync(index, item!, cancellationToken).ConfigureAwait(false);

        if (!loaded.IsHit || loaded.RuntimePackage is null)
        {
            return TrackRuntimePackageCacheResult.Miss;
        }

        TrackRuntimePackage rebuilt = new TrackRuntimePackageRebuilder()
            .RebuildRuntimeOptions(loaded.RuntimePackage, tuning, settingsSchemaVersion);

        return TrackRuntimePackageCacheResult.Hit(rebuilt);
    }

    public async Task<RuntimePackageCacheIndexItem> SaveAsync(
        TrackInput input,
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion,
        TrackRuntimePackage package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(package);

        _pathProvider.EnsureDirectories();
        string analysisCacheKey = RuntimePackageCacheKey.CreateAnalysisKey(identity, tuning, settingsSchemaVersion);
        string branchCacheKey = RuntimePackageCacheKey.CreateBranchKey(identity, tuning, settingsSchemaVersion);
        string runtimeCacheKey = RuntimePackageCacheKey.CreateRuntimeKey(identity, tuning, settingsSchemaVersion);
        string runRoot = package.Files.RunRoot;
        Directory.CreateDirectory(runRoot);
        string manifestPath = Path.Combine(runRoot, "runtime-package.json");
        await _manifestRepository.SaveAsync(package, manifestPath, cancellationToken).ConfigureAwait(false);

        RuntimePackageCacheIndex index = await ReadIndexAsync(cancellationToken).ConfigureAwait(false);
        index.Items.RemoveAll(item => IsRuntimeKeyMatch(item, runtimeCacheKey));

        var indexItem = new RuntimePackageCacheIndexItem
        {
            CacheKey = runtimeCacheKey,
            AnalysisCacheKey = analysisCacheKey,
            BranchCacheKey = branchCacheKey,
            RuntimeCacheKey = runtimeCacheKey,
            FilePath = identity.FilePath,
            FileHash = identity.Sha256,
            FileSizeBytes = identity.FileSizeBytes,
            LastWriteTimeUtc = identity.LastWriteTimeUtc,
            RuntimeManifestPath = manifestPath,
            RunRoot = runRoot,
            CreatedAtUtc = DateTime.UtcNow,
            LastUsedAtUtc = DateTime.UtcNow
        };

        index.Items.Insert(0, indexItem);
        await WriteIndexAsync(index, cancellationToken).ConfigureAwait(false);
        return indexItem;
    }

    private async Task<TrackRuntimePackageCacheResult> TryLoadItemAsync(
        RuntimePackageCacheIndex index,
        RuntimePackageCacheIndexItem item,
        CancellationToken cancellationToken)
    {
        try
        {
            TrackRuntimePackage package = await _manifestRepository
                .LoadAsync(item.RuntimeManifestPath, cancellationToken)
                .ConfigureAwait(false);

            item.LastUsedAtUtc = DateTime.UtcNow;
            await WriteIndexAsync(index, cancellationToken).ConfigureAwait(false);
            return TrackRuntimePackageCacheResult.Hit(package);
        }
        catch (Exception exception) when (item.RuntimeManifestPath.Length > 0)
        {
            string? backupPath = CorruptFileBackup.TryCreate(item.RuntimeManifestPath);
            _logger.Log(
                AppLogLevel.Warning,
                backupPath is null
                    ? "Runtime package manifest could not be loaded. Cache entry will be removed."
                    : $"Runtime package manifest could not be loaded. Backup created at {backupPath}. Cache entry will be removed.",
                exception);
            index.Items.Remove(item);
            await WriteIndexAsync(index, cancellationToken).ConfigureAwait(false);
            return TrackRuntimePackageCacheResult.Miss;
        }
    }

    private static bool IsValidItem(
        RuntimePackageCacheIndexItem? item,
        TrackFileIdentity identity)
    {
        return item is not null
            && File.Exists(item.FilePath)
            && File.Exists(item.RuntimeManifestPath)
            && IsSameFile(item, identity);
    }

    private static bool IsSameFile(
        RuntimePackageCacheIndexItem item,
        TrackFileIdentity identity)
    {
        return string.Equals(item.FileHash, identity.Sha256, StringComparison.OrdinalIgnoreCase)
            && item.FileSizeBytes == identity.FileSizeBytes
            && item.LastWriteTimeUtc == identity.LastWriteTimeUtc;
    }

    private static bool IsRuntimeKeyMatch(
        RuntimePackageCacheIndexItem item,
        string runtimeCacheKey)
    {
        return string.Equals(item.RuntimeCacheKey, runtimeCacheKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.CacheKey, runtimeCacheKey, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<RuntimePackageCacheIndex> ReadIndexAsync(CancellationToken cancellationToken)
    {
        string path = _pathProvider.RuntimeCacheIndexFilePath;

        if (!File.Exists(path))
        {
            return new RuntimePackageCacheIndex();
        }

        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<RuntimePackageCacheIndex>(
                    stream,
                    JsonOptions,
                    cancellationToken)
                    .ConfigureAwait(false)
                ?? new RuntimePackageCacheIndex();
        }
        catch (JsonException exception)
        {
            string? backupPath = CorruptFileBackup.TryCreate(path);
            _logger.Log(
                AppLogLevel.Warning,
                backupPath is null
                    ? "Runtime cache index is corrupt. Cache will be ignored."
                    : $"Runtime cache index is corrupt. Backup created at {backupPath}. Cache will be ignored.",
                exception);
            return new RuntimePackageCacheIndex();
        }
        catch (IOException exception)
        {
            _logger.Log(AppLogLevel.Warning, "Runtime cache index could not be read. Cache will be ignored.", exception);
            return new RuntimePackageCacheIndex();
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.Log(AppLogLevel.Warning, "Runtime cache index could not be accessed. Cache will be ignored.", exception);
            return new RuntimePackageCacheIndex();
        }
    }

    private async Task WriteIndexAsync(
        RuntimePackageCacheIndex index,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_pathProvider.RuntimeCacheIndexFilePath)!);
        await using FileStream stream = File.Create(_pathProvider.RuntimeCacheIndexFilePath);
        await JsonSerializer.SerializeAsync(stream, index, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
