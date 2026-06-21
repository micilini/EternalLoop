namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed class BeatGridGuardrailOptions
{
    public int MinBeatCount { get; init; } = 2;

    public double MinBpm { get; init; } = 40.0;

    public double MaxBpm { get; init; } = 220.0;

    public double MaxBeatsPerSecond { get; init; } = 6.5;

    public double MaxBeatIntervalStdDevRatio { get; init; } = 0.65;

    public double MaxDownbeatToBeatDistanceSeconds { get; init; } = 0.03;

    public double MinMeanConfidence { get; init; } = 0.01;

    public double MinAiCoverageRatio { get; init; } = 0.80;
}
