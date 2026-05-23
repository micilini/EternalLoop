namespace EternalLoop.Contracts.Models;

public sealed class BeatMicrosegment
{
    public required int BeatIndex { get; init; }

    public required int SegmentIndex { get; init; }

    public required double Start { get; init; }

    public required double Duration { get; init; }

    public required float RelativePosition { get; init; }

    public required float[] Timbre { get; init; }

    public required float[] Pitches { get; init; }

    public required float[] Loudness { get; init; }

    public required float Flux { get; init; }
}
