namespace EternalLoop.Contracts.Models;

public sealed class JukeboxNode
{
    public required int BeatIndex { get; init; }

    public required double Start { get; init; }

    public required double Duration { get; init; }
}
