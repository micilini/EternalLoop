namespace EternalLoop.BranchAnalysis.Core.Distance;

public sealed class SegmentDistanceWeights
{
    public static SegmentDistanceWeights Default { get; } = new();

    public double Timbre { get; init; } = 1;
    public double Pitch { get; init; } = 10;
    public double LoudStart { get; init; } = 1;
    public double LoudMax { get; init; } = 1;
    public double Duration { get; init; } = 100;
    public double Confidence { get; init; } = 1;
}
