namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class BranchTopologyResult
{
    public int RemovedBranches { get; init; }
    public int LocalLoopRiskBranches { get; init; }
}
