namespace EternalLoop.Core.Workflow;

public sealed record TrackRuntimePreparationSummary(
    int RuntimeBeatCount,
    int RuntimeBranchCount,
    bool IsPlayable,
    int IgnoredActiveBranches = 0,
    int IgnoredCandidateBranches = 0)
{
    public bool HasRuntimeBranches => RuntimeBranchCount > 0;
}
