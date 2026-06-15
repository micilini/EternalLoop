using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class LateAnchorRoutingResult
{
    public int LastBranchPoint { get; init; } = -1;

    public double LongestReach { get; init; }

    public BranchEdge? InsertedEdge { get; init; }

    public BranchEdge? SelectedAnchorEdge { get; init; }

    public string Decision { get; init; } = "none";

    public string Reason { get; init; } = "no-anchor";

    public int EarlyReturnTargetBeat { get; init; }

    public int BranchesToEarlyReturnTarget { get; init; } = int.MaxValue;

    public int EarliestReachableBeat { get; init; } = int.MaxValue;

    public int ImmediateBackwardBeats { get; init; }

    public double AnchorDistance { get; init; } = double.PositiveInfinity;
}
