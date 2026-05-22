namespace EternalLoop.Contracts.Models;

public sealed class BeatMicroFingerprint
{
    public required int BeatIndex { get; init; }

    public required IReadOnlyList<BeatMicrosegment> Microsegments { get; init; }
}
