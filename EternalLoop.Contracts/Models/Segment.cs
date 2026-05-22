namespace EternalLoop.Contracts.Models;

public sealed class Segment
{
    public required double Start { get; init; }

    public required double Duration { get; init; }

    public required double Confidence { get; init; }

    public required double LoudnessStart { get; init; }

    public required double LoudnessMax { get; init; }

    public required double LoudnessMaxTime { get; init; }

    public required float[] Timbre { get; init; }

    public required float[] Pitches { get; init; }
}
