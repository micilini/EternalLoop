namespace EternalLoop.Playback.Runtime;

public sealed class BranchEscapeResult
{
    public bool IsSafe { get; init; }

    public string Reason { get; init; } = string.Empty;

    public int DestinationBeatIndex { get; init; }

    public bool DestinationIsInEndZone { get; init; }

    public bool HasFutureEscape { get; init; }
}
