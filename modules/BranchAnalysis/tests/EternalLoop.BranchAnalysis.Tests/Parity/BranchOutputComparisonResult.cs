namespace EternalLoop.BranchAnalysis.Tests.Parity;

public sealed class BranchOutputComparisonResult
{
    public IReadOnlyList<BranchOutputDifference> Differences { get; init; } = [];
    public bool AreEqual => Differences.Count == 0;

    public string ToFailureMessage()
    {
        return string.Join(Environment.NewLine, Differences.Select(difference => difference.Message));
    }
}
