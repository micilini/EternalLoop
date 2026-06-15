using EternalLoop.Playback.Visualization;

namespace EternalLoop.App.Controls;

public sealed class LoopMapRenderPlan
{
    public const int MaxDisplayedEdges = 650;
    public const int HighlightedEdgeCount = 12;

    private readonly IReadOnlyDictionary<int, IReadOnlyList<BranchGraphEdge>> _highlightedEdgesByBeat;

    private LoopMapRenderPlan(
        IReadOnlyDictionary<int, int> beatOrdinals,
        IReadOnlyList<BranchGraphEdge> displayEdges,
        IReadOnlyDictionary<int, IReadOnlyList<BranchGraphEdge>> highlightedEdgesByBeat)
    {
        BeatOrdinals = beatOrdinals;
        DisplayEdges = displayEdges;
        _highlightedEdgesByBeat = highlightedEdgesByBeat;
    }

    public static LoopMapRenderPlan Empty { get; } = new(
        new Dictionary<int, int>(),
        [],
        new Dictionary<int, IReadOnlyList<BranchGraphEdge>>());

    public IReadOnlyDictionary<int, int> BeatOrdinals { get; }

    public IReadOnlyList<BranchGraphEdge> DisplayEdges { get; }

    public static LoopMapRenderPlan Create(BranchGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.Nodes.Count == 0)
        {
            return Empty;
        }

        return new LoopMapRenderPlan(
            CreateBeatOrdinals(graph),
            CreateDisplayEdges(graph),
            CreateHighlightedEdgesByBeat(graph));
    }

    public bool TryGetHighlightedEdges(
        int beatIndex,
        out IReadOnlyList<BranchGraphEdge> edges)
    {
        return _highlightedEdgesByBeat.TryGetValue(beatIndex, out edges!);
    }

    public static double QualityFromDistance(double distance)
    {
        double quality = !double.IsFinite(distance)
            ? 0.35
            : distance <= 0
                ? 1.0
                : 1.0 / (1.0 + distance);

        return Math.Clamp(quality, 0.15, 1.0);
    }

    private static IReadOnlyDictionary<int, int> CreateBeatOrdinals(BranchGraph graph)
    {
        Dictionary<int, int> ordinals = new(graph.Nodes.Count);

        for (int index = 0; index < graph.Nodes.Count; index++)
        {
            ordinals[graph.Nodes[index].BeatIndex] = index;
        }

        return ordinals;
    }

    private static IReadOnlyList<BranchGraphEdge> CreateDisplayEdges(BranchGraph graph)
    {
        return graph.Edges
            .Where(edge => edge.FromBeat != edge.ToBeat)
            .OrderByDescending(edge => QualityFromDistance(edge.Distance))
            .ThenBy(edge => edge.Id)
            .Take(MaxDisplayedEdges)
            .OrderBy(edge => QualityFromDistance(edge.Distance))
            .ThenBy(edge => edge.Id)
            .ToArray();
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<BranchGraphEdge>> CreateHighlightedEdgesByBeat(
        BranchGraph graph)
    {
        Dictionary<int, IReadOnlyList<BranchGraphEdge>> edgesByBeat = [];
        Dictionary<int, List<BranchGraphEdge>> pendingEdgesByBeat = [];

        foreach (BranchGraphEdge edge in graph.Edges)
        {
            if (edge.FromBeat == edge.ToBeat)
            {
                continue;
            }

            if (!pendingEdgesByBeat.TryGetValue(edge.FromBeat, out List<BranchGraphEdge>? edges))
            {
                edges = [];
                pendingEdgesByBeat[edge.FromBeat] = edges;
            }

            edges.Add(edge);
        }

        foreach (KeyValuePair<int, List<BranchGraphEdge>> item in pendingEdgesByBeat)
        {
            edgesByBeat[item.Key] = item.Value
                .OrderByDescending(edge => QualityFromDistance(edge.Distance))
                .ThenBy(edge => edge.ToBeat)
                .ThenBy(edge => edge.Id)
                .Take(HighlightedEdgeCount)
                .ToArray();
        }

        return edgesByBeat;
    }
}
