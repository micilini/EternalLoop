namespace EternalLoop.Core.Settings;

public sealed record LoopTuningPresetDefinition(
    string Id,
    string Description,
    double SimilarityThreshold,
    int LookaheadDepth,
    int MinJumpDistance,
    int MaxBranchesPerBeat,
    double JumpProbability,
    int JumpCooldown,
    double FirstPassLinearPlaybackRatio,
    string BranchQuantumType,
    int BranchMaxThreshold,
    bool AnalysisMusicalQuality);
