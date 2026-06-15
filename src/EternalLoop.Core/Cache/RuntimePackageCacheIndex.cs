namespace EternalLoop.Core.Cache;

public sealed class RuntimePackageCacheIndex
{
    public int SchemaVersion { get; init; } = 1;

    public List<RuntimePackageCacheIndexItem> Items { get; init; } = [];
}
