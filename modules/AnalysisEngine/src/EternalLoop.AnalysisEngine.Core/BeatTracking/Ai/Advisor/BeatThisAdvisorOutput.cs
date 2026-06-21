namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;

public sealed class BeatThisAdvisorOutput
{
    public required float[] BeatLogits { get; init; }

    public required float[] DownbeatLogits { get; init; }

    public required int FrameCount { get; init; }

    public required double FrameRate { get; init; }

    public required double DurationSeconds { get; init; }

    public required int ChunkCount { get; init; }

    public required string OutputMode { get; init; }

    public required string AggregatePolicy { get; init; }
}
