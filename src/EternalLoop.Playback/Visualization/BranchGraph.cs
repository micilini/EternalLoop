namespace EternalLoop.Playback.Visualization;

public sealed class BranchGraph
{
    public static BranchGraph Empty { get; } = new()
    {
        Nodes = [],
        Edges = [],
        TotalBeatCount = 0,
        DisplayedEdgeCount = 0,
        HiddenEdgeCount = 0
    };

    public required IReadOnlyList<BranchGraphNode> Nodes { get; init; }

    public required IReadOnlyList<BranchGraphEdge> Edges { get; init; }

    public int TotalBeatCount { get; init; }

    public int DisplayedEdgeCount { get; init; }

    public int HiddenEdgeCount { get; init; }
}
