namespace EternalLoop.Contracts.Models;

public sealed class BeatTrackingResult
{
    public required double EstimatedBpm { get; init; }

    public required double[] BeatTimes { get; init; }

    public required double[] Confidences { get; init; }
}
