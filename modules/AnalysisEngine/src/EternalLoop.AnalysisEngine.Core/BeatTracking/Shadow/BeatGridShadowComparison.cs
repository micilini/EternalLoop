namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;

public sealed class BeatGridShadowComparison
{
    public double? CountRatio { get; init; }

    public double? BpmDelta { get; init; }

    public double Precision50Ms { get; init; }

    public double Recall50Ms { get; init; }

    public double F1_50Ms { get; init; }

    public double Precision70Ms { get; init; }

    public double Recall70Ms { get; init; }

    public double F1_70Ms { get; init; }

    public double Precision100Ms { get; init; }

    public double Recall100Ms { get; init; }

    public double F1_100Ms { get; init; }

    public double? BestOffsetMs { get; init; }

    public double? BestOffsetF1_70Ms { get; init; }
}
