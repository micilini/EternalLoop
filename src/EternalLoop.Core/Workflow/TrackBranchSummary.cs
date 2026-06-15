namespace EternalLoop.Core.Workflow;

public sealed record TrackBranchSummary(
    int ActiveBranchCount,
    int CandidateBranchCount,
    int IgnoredActiveBranchCount = 0,
    int IgnoredCandidateBranchCount = 0)
{
    public bool HasActiveBranches => ActiveBranchCount > 0;
}
