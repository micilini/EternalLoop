namespace EternalLoop.Playback.Audio;

public sealed class BranchJumpEventArgs : EventArgs
{
    public int FromBeatIndex { get; init; }

    public int SeedBeatIndex { get; init; }

    public int ToBeatIndex { get; init; }

    public int BranchId { get; init; }

    public double Distance { get; init; }

    public double ChanceBeforeDecision { get; init; }

    public double RandomValue { get; init; }

    public bool ForcedEndGuardJump { get; init; }

    public string Reason { get; init; } = string.Empty;
}
