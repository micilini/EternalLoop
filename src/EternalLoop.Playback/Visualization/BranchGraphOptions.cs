namespace EternalLoop.Playback.Visualization;

public sealed class BranchGraphOptions
{
    public int MaxDisplayedEdges { get; init; } = 650;

    public bool PreferLowDistanceEdges { get; init; } = true;

    public int? CurrentBeatIndex { get; init; }

    public int? LastJumpFromBeat { get; init; }

    public int? LastJumpToBeat { get; init; }
}
