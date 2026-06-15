namespace EternalLoop.Playback.Visualization;

public sealed class BranchGraphNode
{
    public int BeatIndex { get; init; }

    public double Start { get; init; }

    public double Duration { get; init; }

    public double Confidence { get; init; }

    public double AngleRadians { get; init; }

    public bool IsCurrent { get; init; }

    public bool IsLastJumpEndpoint { get; init; }
}
