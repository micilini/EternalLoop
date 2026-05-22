using System.Collections.Generic;

namespace EternalLoop.Contracts.Models;

public sealed class JukeboxGraph
{
    public required IReadOnlyList<JukeboxNode> Nodes { get; init; }

    public required IReadOnlyDictionary<int, List<JukeboxEdge>> JumpEdges { get; init; }

    public required double SimilarityThreshold { get; init; }

    public required int LookaheadDepth { get; init; }
}
