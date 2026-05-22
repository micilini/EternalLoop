using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.JukeboxEngine;

internal static class JukeboxGraphBuilder
{
    public static JukeboxGraph Build(
        IReadOnlyList<Beat> beats,
        IReadOnlyList<JukeboxEdge> edges,
        BranchFindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(options);

        var nodes = beats
            .OrderBy(beat => beat.Index)
            .Select(beat => new JukeboxNode
            {
                BeatIndex = beat.Index,
                Start = beat.Start,
                Duration = beat.Duration
            })
            .ToArray();

        var validBeatIndexes = nodes
            .Select(node => node.BeatIndex)
            .ToHashSet();

        var groupedEdges = edges
            .Where(edge =>
                validBeatIndexes.Contains(edge.FromBeat) &&
                validBeatIndexes.Contains(edge.ToBeat) &&
                edge.FromBeat != edge.ToBeat)
            .GroupBy(edge => edge.FromBeat)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(edge => edge.Similarity)
                    .ThenBy(edge => edge.ToBeat)
                    .ToList());

        return new JukeboxGraph
        {
            Nodes = nodes,
            JumpEdges = groupedEdges,
            SimilarityThreshold = options.SimilarityThreshold,
            LookaheadDepth = options.LookaheadDepth
        };
    }
}
