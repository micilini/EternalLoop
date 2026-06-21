namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;

public sealed class BeatThisAdvisorPostprocessOptions
{
    public int LocalMaximaWindowFrames { get; init; } = 1;

    public double BeatThresholdPercentile { get; init; } = 0.90;

    public double DownbeatThresholdPercentile { get; init; } = 0.95;

    public double MinBeatSpacingSeconds { get; init; } = 0.25;

    public double MinDownbeatSpacingSeconds { get; init; } = 1.0;

    public double MinBpm { get; init; } = 60.0;

    public double MaxBpm { get; init; } = 200.0;

    public double MaxBeatDensityPerSecond { get; init; } = 4.0;

    public double MaxCountRatio { get; init; } = 1.5;

    public double MinMedianIntervalSeconds { get; init; } = 0.25;

    public int? ReferenceBeatCount { get; init; }
}
