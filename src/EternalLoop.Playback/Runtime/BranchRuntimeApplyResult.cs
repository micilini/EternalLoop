namespace EternalLoop.Playback.Runtime;

public sealed record BranchRuntimeApplyResult(
    int AppliedActiveBranches,
    int AppliedCandidateBranches,
    int IgnoredActiveBranches,
    int IgnoredCandidateBranches);
