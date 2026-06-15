using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Visualization;

public sealed class BranchGraphBuilder
{
    private const int DefaultMaxDisplayedEdges = 650;

    public BranchGraph Build(RuntimeTrack? track, BranchGraphOptions? options = null)
    {
        if (track is null || track.Beats.Count == 0)
        {
            return BranchGraph.Empty;
        }

        BranchGraphOptions resolvedOptions = options ?? new BranchGraphOptions();
        int maxDisplayedEdges = resolvedOptions.MaxDisplayedEdges <= 0
            ? DefaultMaxDisplayedEdges
            : resolvedOptions.MaxDisplayedEdges;

        List<BranchGraphNode> nodes = CreateNodes(track.Beats, resolvedOptions);
        List<BranchGraphEdge> allEdges = CreateEdges(track, resolvedOptions);
        List<BranchGraphEdge> sortedEdges = SortEdges(allEdges, resolvedOptions.PreferLowDistanceEdges);
        List<BranchGraphEdge> displayedEdges = sortedEdges.Take(maxDisplayedEdges).ToList();

        return new BranchGraph
        {
            Nodes = nodes,
            Edges = displayedEdges,
            TotalBeatCount = nodes.Count,
            DisplayedEdgeCount = displayedEdges.Count,
            HiddenEdgeCount = Math.Max(0, sortedEdges.Count - displayedEdges.Count)
        };
    }

    private static List<BranchGraphNode> CreateNodes(
        IReadOnlyList<RuntimeBeat> beats,
        BranchGraphOptions options)
    {
        List<BranchGraphNode> nodes = new(beats.Count);

        for (int index = 0; index < beats.Count; index++)
        {
            RuntimeBeat beat = beats[index];

            nodes.Add(new BranchGraphNode
            {
                BeatIndex = beat.Which,
                Start = beat.Start,
                Duration = beat.Duration,
                Confidence = beat.Confidence,
                AngleRadians = ((double)index / beats.Count * Math.Tau) - (Math.PI / 2),
                IsCurrent = options.CurrentBeatIndex == beat.Which,
                IsLastJumpEndpoint = options.LastJumpFromBeat == beat.Which
                    || options.LastJumpToBeat == beat.Which
            });
        }

        return nodes;
    }

    private static List<BranchGraphEdge> CreateEdges(RuntimeTrack track, BranchGraphOptions options)
    {
        List<BranchGraphEdge> edges = [];
        HashSet<int> beatIndexes = track.Beats.Select(beat => beat.Which).ToHashSet();

        foreach (RuntimeBeat beat in track.Beats)
        {
            foreach (RuntimeBranchEdge neighbor in beat.Neighbors)
            {
                if (neighbor.Deleted
                    || !beatIndexes.Contains(neighbor.FromBeat)
                    || !beatIndexes.Contains(neighbor.ToBeat)
                    || neighbor.FromBeat == neighbor.ToBeat)
                {
                    continue;
                }

                if (!double.IsFinite(neighbor.Distance))
                {
                    continue;
                }

                edges.Add(new BranchGraphEdge
                {
                    Id = neighbor.Id,
                    FromBeat = neighbor.FromBeat,
                    ToBeat = neighbor.ToBeat,
                    JumpBeats = neighbor.JumpBeats,
                    Direction = neighbor.Direction,
                    Distance = neighbor.Distance,
                    IsLastJump = options.LastJumpFromBeat == neighbor.FromBeat
                        && options.LastJumpToBeat == neighbor.ToBeat
                });
            }
        }

        return edges;
    }

    private static List<BranchGraphEdge> SortEdges(IReadOnlyList<BranchGraphEdge> edges, bool preferLowDistanceEdges)
    {
        IOrderedEnumerable<BranchGraphEdge> orderedEdges = preferLowDistanceEdges
            ? edges.OrderBy(edge => edge.Distance)
            : edges.OrderBy(edge => edge.FromBeat);

        return orderedEdges
            .ThenBy(edge => edge.FromBeat)
            .ThenBy(edge => edge.ToBeat)
            .ThenBy(edge => edge.Id)
            .ToList();
    }
}
