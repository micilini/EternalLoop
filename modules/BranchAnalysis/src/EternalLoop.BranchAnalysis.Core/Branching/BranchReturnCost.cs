namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class BranchReturnCost
{
    public int BranchesToEarlyReturnTarget { get; init; }

    public int EarliestReachable { get; init; }

    public int ImmediateBackwardBeats { get; init; }

    public double AcousticDistance { get; init; }
}
