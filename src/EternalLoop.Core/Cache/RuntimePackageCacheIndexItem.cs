namespace EternalLoop.Core.Cache;

public sealed class RuntimePackageCacheIndexItem
{
    public string CacheKey { get; init; } = string.Empty;

    public string AnalysisCacheKey { get; init; } = string.Empty;

    public string BranchCacheKey { get; init; } = string.Empty;

    public string RuntimeCacheKey { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public string FileHash { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }

    public DateTime LastWriteTimeUtc { get; init; }

    public string RuntimeManifestPath { get; init; } = string.Empty;

    public string RunRoot { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime LastUsedAtUtc { get; set; }
}
