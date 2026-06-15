namespace EternalLoop.Playback.Models;

public sealed class RuntimeBranchEdge
{
    public int Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public int FromBeat { get; init; }

    public int ToBeat { get; init; }

    public int JumpBeats { get; init; }

    public string Direction { get; init; } = string.Empty;

    public double Distance { get; init; }

    public bool Deleted { get; init; }

    public required RuntimeBeat SourceBeat { get; init; }

    public required RuntimeBeat DestinationBeat { get; init; }
}
