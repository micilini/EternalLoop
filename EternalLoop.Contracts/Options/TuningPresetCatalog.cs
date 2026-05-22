using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;

namespace EternalLoop.Contracts.Options;

public static class TuningPresetCatalog
{
    public const string ConservativeId = "Conservative";
    public const string BalancedId = "Balanced";
    public const string WildId = "Wild";

    public static IReadOnlyList<TuningPresetDefinition> All { get; } =
    [
        new TuningPresetDefinition
        {
            Id = ConservativeId,
            Name = "Conservative",
            Description = "Few jumps, stricter phrase matches, safest listening experience.",
            SimilarityThreshold = 0.92,
            LookaheadDepth = 5,
            MinJumpDistance = 28,
            MaxBranchesPerBeat = 2,
            JumpProbability = 0.14,
            JumpCooldown = 16,
            FirstPassLinearPlaybackRatio = 0.82,
            UseDurationSimilarityGate = true,
            DurationPenaltyStartRatio = 0.94,
            DurationRejectionRatio = 0.86,
            DurationPenaltyStrength = 0.35,
            UseConfidencePenalty = true,
            ConfidencePenaltyStart = 0.60,
            ConfidenceRejectionThreshold = 0.35,
            ConfidencePenaltyStrength = 0.30,
            MetricPositionMode = MetricPositionMode.StrictGate,
            MetricPositionPenaltyStrength = 0.45,
            MetricPositionRejectionThreshold = 0.95,
            TargetBranchSourceRatio = 0.10,
            MaxBranchSourceRatio = 0.14,
            UseMicrosegmentSimilarity = true,
            MicrosegmentCount = 4,
            MicrosegmentPenaltyStartThreshold = 0.86,
            MicrosegmentRejectionThreshold = 0.76,
            MicrosegmentPenaltyStrength = 0.35
        },
        new TuningPresetDefinition
        {
            Id = BalancedId,
            Name = "Balanced",
            Description = "Default EternalLoop behavior: musical, safe and controlled.",
            SimilarityThreshold = 0.86,
            LookaheadDepth = 4,
            MinJumpDistance = 20,
            MaxBranchesPerBeat = 3,
            JumpProbability = 0.22,
            JumpCooldown = 12,
            FirstPassLinearPlaybackRatio = 0.78,
            UseDurationSimilarityGate = true,
            DurationPenaltyStartRatio = 0.90,
            DurationRejectionRatio = 0.80,
            DurationPenaltyStrength = 0.25,
            UseConfidencePenalty = true,
            ConfidencePenaltyStart = 0.50,
            ConfidenceRejectionThreshold = 0.25,
            ConfidencePenaltyStrength = 0.20,
            MetricPositionMode = MetricPositionMode.StrongPenalty,
            MetricPositionPenaltyStrength = 0.32,
            MetricPositionRejectionThreshold = 0.20,
            TargetBranchSourceRatio = 0.16,
            MaxBranchSourceRatio = 0.22,
            UseMicrosegmentSimilarity = true,
            MicrosegmentCount = 4,
            MicrosegmentPenaltyStartThreshold = 0.82,
            MicrosegmentRejectionThreshold = 0.70,
            MicrosegmentPenaltyStrength = 0.25
        },
        new TuningPresetDefinition
        {
            Id = WildId,
            Name = "Wild",
            Description = "More jumps, but still phrase-safe. Useful for tracks with few branches.",
            SimilarityThreshold = 0.78,
            LookaheadDepth = 3,
            MinJumpDistance = 12,
            MaxBranchesPerBeat = 5,
            JumpProbability = 0.42,
            JumpCooldown = 6,
            FirstPassLinearPlaybackRatio = 0.70,
            UseDurationSimilarityGate = true,
            DurationPenaltyStartRatio = 0.84,
            DurationRejectionRatio = 0.72,
            DurationPenaltyStrength = 0.15,
            UseConfidencePenalty = true,
            ConfidencePenaltyStart = 0.40,
            ConfidenceRejectionThreshold = 0.15,
            ConfidencePenaltyStrength = 0.12,
            MetricPositionMode = MetricPositionMode.SoftPenalty,
            MetricPositionPenaltyStrength = 0.16,
            MetricPositionRejectionThreshold = 0.0,
            TargetBranchSourceRatio = 0.25,
            MaxBranchSourceRatio = 0.34,
            UseMicrosegmentSimilarity = true,
            MicrosegmentCount = 3,
            MicrosegmentPenaltyStartThreshold = 0.76,
            MicrosegmentRejectionThreshold = 0.62,
            MicrosegmentPenaltyStrength = 0.16
        }
    ];

    public static TuningPresetDefinition GetById(string? id)
    {
        return All.FirstOrDefault(preset =>
            string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? All.First(preset => preset.Id == BalancedId);
    }
}
