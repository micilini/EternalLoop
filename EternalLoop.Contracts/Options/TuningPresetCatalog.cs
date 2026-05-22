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
            FirstPassLinearPlaybackRatio = 0.82
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
            FirstPassLinearPlaybackRatio = 0.78
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
            FirstPassLinearPlaybackRatio = 0.70
        }
    ];

    public static TuningPresetDefinition GetById(string? id)
    {
        return All.FirstOrDefault(preset =>
            string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? All.First(preset => preset.Id == BalancedId);
    }
}
