namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;

public sealed class BeatThisAdvisorPostprocessResult
{
    public required double[] BeatTimes { get; init; }

    public required double[] DownbeatTimes { get; init; }

    public required double[] BeatConfidences { get; init; }

    public required double EstimatedBpm { get; init; }

    public required bool IsDenseGrid { get; init; }

    public string? RejectionReason { get; init; }

    public required string Transform { get; init; }

    public required string Algorithm { get; init; }
}
