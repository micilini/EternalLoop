namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class BranchTopologyPolicyException : Exception
{
    public BranchTopologyPolicyException(string message)
        : base(message)
    {
    }
}
