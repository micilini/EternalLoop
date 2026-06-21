namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisOutputTensor
{
    public required string Name { get; init; }

    public required float[] Data { get; init; }

    public required int[] Dimensions { get; init; }
}
