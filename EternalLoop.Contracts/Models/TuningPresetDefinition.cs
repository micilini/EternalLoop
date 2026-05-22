using EternalLoop.Contracts.Enums;

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

    public required bool UseDurationSimilarityGate { get; init; }

    public required double DurationPenaltyStartRatio { get; init; }

    public required double DurationRejectionRatio { get; init; }

    public required double DurationPenaltyStrength { get; init; }

    public required bool UseConfidencePenalty { get; init; }

    public required double ConfidencePenaltyStart { get; init; }

    public required double ConfidenceRejectionThreshold { get; init; }

    public required double ConfidencePenaltyStrength { get; init; }

    public required MetricPositionMode MetricPositionMode { get; init; }

    public required double MetricPositionPenaltyStrength { get; init; }

    public required double MetricPositionRejectionThreshold { get; init; }

    public required double TargetBranchSourceRatio { get; init; }

    public required double MaxBranchSourceRatio { get; init; }

    public required bool UseMicrosegmentSimilarity { get; init; }

    public required int MicrosegmentCount { get; init; }

    public required double MicrosegmentPenaltyStartThreshold { get; init; }

    public required double MicrosegmentRejectionThreshold { get; init; }

    public required double MicrosegmentPenaltyStrength { get; init; }
}
