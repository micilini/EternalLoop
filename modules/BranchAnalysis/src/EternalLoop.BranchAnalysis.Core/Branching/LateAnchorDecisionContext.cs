namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class LateAnchorDecisionContext
{
    public required int EarlyReturnTargetBeat { get; init; }

    public required IReadOnlyDictionary<int, int> BranchesToEarlyReturnTarget { get; init; }

    public required IReadOnlyDictionary<int, int> EarliestReachableByBeat { get; init; }
}
