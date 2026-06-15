using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class LateAnchorSourceCandidate
{
    public required int SourceIndex { get; init; }

    public required TimeQuantum Source { get; init; }

    public required BranchEdge Edge { get; init; }

    public required BranchReturnCost Cost { get; init; }
}
