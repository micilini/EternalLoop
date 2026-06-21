namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisInferenceResult
{
    public required float[] BeatActivations { get; init; }

    public required float[] DownbeatActivations { get; init; }

    public required double FrameRate { get; init; }

    public required int ValidFrameCount { get; init; }

    public int StartFrameIndex { get; init; }

    public double StartTimeSeconds { get; init; }

    public double AudioDurationSeconds { get; init; }

    public int ChunkCount { get; init; } = 1;

    public string[] OutputNames { get; init; } = [];

    public string OutputMode { get; init; } = "unknown";
}
