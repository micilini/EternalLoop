namespace EternalLoop.Playback.Runtime;

public sealed class BranchDecisionOptions
{
    public bool InfiniteMode { get; init; } = true;

    public double MinRandomBranchChance { get; init; } = 0.18;

    public double MaxRandomBranchChance { get; init; } = 0.50;

    public double RandomBranchChanceDelta { get; init; } = 0.018;

    public double JumpProbability { get; init; } = 0.22;

    public int JumpCooldownBeats { get; init; } = 12;

    public double FirstPassLinearPlaybackRatio { get; init; } = 0.78;

    public bool RotateBranches { get; init; } = true;

    public bool EnableJumpShapingKnobs { get; init; } = true;

    public bool NormalizeChanceDeltaByTempo { get; init; } = true;

    public bool WeightedBranchSelection { get; init; } = true;

    public double RepeatPenalty { get; init; } = 0.35;

    public BranchEscapeOptions EscapeOptions { get; init; } = new();
}
