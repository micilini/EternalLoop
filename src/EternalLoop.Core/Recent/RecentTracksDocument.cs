namespace EternalLoop.Core.Recent;

public sealed class RecentTracksDocument
{
    public int SchemaVersion { get; init; } = 1;

    public List<RecentTrackEntry> Items { get; init; } = [];
}
