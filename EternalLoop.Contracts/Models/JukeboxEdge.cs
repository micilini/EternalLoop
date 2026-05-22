namespace EternalLoop.Contracts.Models;

public sealed class JukeboxEdge
{
    public required int FromBeat { get; init; }

    public required int ToBeat { get; init; }

    public required double Similarity { get; init; }
}
