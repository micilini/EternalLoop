namespace EternalLoop.Contracts.Models;

public sealed class TuningPresetDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required double SimilarityThreshold { get; init; }

    public required int LookaheadDepth { get; init; }

    public required int MinJumpDistance { get; init; }

    public required int MaxBranchesPerBeat { get; init; }

    public required double JumpProbability { get; init; }

    public required int JumpCooldown { get; init; }

    public required double FirstPassLinearPlaybackRatio { get; init; }
}
