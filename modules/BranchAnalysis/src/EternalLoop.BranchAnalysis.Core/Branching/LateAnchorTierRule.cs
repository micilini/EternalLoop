namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class LateAnchorTierRule
{
    public required int MaxAdditionalBranches { get; init; }

    public required int MinImmediateBackwardBeats { get; init; }
}
