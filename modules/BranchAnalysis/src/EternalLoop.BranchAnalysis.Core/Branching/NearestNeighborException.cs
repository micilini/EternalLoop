namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class NearestNeighborException : Exception
{
    public NearestNeighborException(string message)
        : base(message)
    {
    }
}
