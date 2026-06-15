namespace EternalLoop.Core.Settings;

public static class LoopTuningPresetCatalog
{
    public const string ConservativeId = "Conservative";

    public const string BalancedId = "Balanced";

    public const string WildId = "Wild";

    private static readonly LoopTuningPresetDefinition Conservative = new(
        ConservativeId,
        "Few jumps, stricter phrase matches, safest listening experience.",
        SimilarityThreshold: 0.92,
        LookaheadDepth: 2,
        MinJumpDistance: 16,
        MaxBranchesPerBeat: 2,
        JumpProbability: 0.14,
        JumpCooldown: 16,
        FirstPassLinearPlaybackRatio: 0.82,
        BranchQuantumType: "beats",
        BranchMaxThreshold: 70,
        AnalysisMusicalQuality: true);

    private static readonly LoopTuningPresetDefinition Balanced = new(
        BalancedId,
        "Default EternalLoop behavior: musical, safe and controlled.",
        SimilarityThreshold: 0.86,
        LookaheadDepth: 1,
        MinJumpDistance: 4,
        MaxBranchesPerBeat: 4,
        JumpProbability: 0.22,
        JumpCooldown: 12,
        FirstPassLinearPlaybackRatio: 0.78,
        BranchQuantumType: "beats",
        BranchMaxThreshold: 80,
        AnalysisMusicalQuality: true);

    private static readonly LoopTuningPresetDefinition Wild = new(
        WildId,
        "More jumps, but still phrase-safe. Useful for tracks with few branches.",
        SimilarityThreshold: 0.78,
        LookaheadDepth: 1,
        MinJumpDistance: 4,
        MaxBranchesPerBeat: 6,
        JumpProbability: 0.42,
        JumpCooldown: 6,
        FirstPassLinearPlaybackRatio: 0.70,
        BranchQuantumType: "beats",
        BranchMaxThreshold: 95,
        AnalysisMusicalQuality: true);

    public static IReadOnlyList<LoopTuningPresetDefinition> All { get; } =
    [
        Conservative,
        Balanced,
        Wild
    ];

    public static LoopTuningPresetDefinition GetById(string? id)
    {
        return All.FirstOrDefault(preset =>
                string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? Balanced;
    }

    public static void ApplyPreset(
        LoopTuningSettings settings,
        LoopTuningPresetDefinition preset)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(preset);

        settings.Preset = preset.Id;
        settings.SimilarityThreshold = preset.SimilarityThreshold;
        settings.LookaheadDepth = preset.LookaheadDepth;
        settings.MinJumpDistance = preset.MinJumpDistance;
        settings.MaxBranchesPerBeat = preset.MaxBranchesPerBeat;
        settings.JumpProbability = preset.JumpProbability;
        settings.JumpCooldown = preset.JumpCooldown;
        settings.FirstPassLinearPlaybackRatio = preset.FirstPassLinearPlaybackRatio;
        settings.BranchQuantumType = preset.BranchQuantumType;
        settings.BranchMaxThreshold = preset.BranchMaxThreshold;
        settings.AnalysisMusicalQuality = preset.AnalysisMusicalQuality;
    }
}
