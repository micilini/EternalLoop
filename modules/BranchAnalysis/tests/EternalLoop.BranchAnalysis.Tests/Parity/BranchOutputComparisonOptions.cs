namespace EternalLoop.BranchAnalysis.Tests.Parity;

public sealed class BranchOutputComparisonOptions
{
    public double NumericTolerance { get; init; } = 0.000001;
    public int DecimalPlaces { get; init; } = 6;
}
