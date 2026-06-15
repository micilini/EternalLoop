namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class PostProcessException : Exception
{
    public PostProcessException(string message)
        : base(message)
    {
    }
}
