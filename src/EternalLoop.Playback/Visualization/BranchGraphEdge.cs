namespace EternalLoop.Playback.Visualization;

public sealed class BranchGraphEdge
{
    public int Id { get; init; }

    public int FromBeat { get; init; }

    public int ToBeat { get; init; }

    public int JumpBeats { get; init; }

    public string Direction { get; init; } = string.Empty;

    public double Distance { get; init; }

    public bool IsLastJump { get; init; }
}
