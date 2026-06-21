namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisPostprocessorOptions
{
    public double BeatThreshold { get; init; } = 0.08;

    public double DownbeatThreshold { get; init; } = 0.05;

    public double AdaptivePeakPercentile { get; init; } = 0.90;

    public double MinBeatSpacingSeconds { get; init; } = 0.30;

    public double MinDownbeatSpacingSeconds { get; init; } = 0.75;

    public double MaxDownbeatSnapDistanceSeconds { get; init; } = 0.18;

    public int DefaultMeter { get; init; } = 4;

    public int[] MeterCandidates { get; init; } = [3, 4, 6];

    public double MinBpm { get; init; } = 40.0;

    public double MaxBpm { get; init; } = 220.0;

    public double FallbackBpm { get; init; } = 120.0;
}
