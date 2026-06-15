namespace EternalLoop.Core.Runtime;

public sealed record TrackRuntimeTuningSnapshot(
    string Preset,
    double SimilarityThreshold,
    int LookaheadDepth,
    int MinJumpDistance,
    int MaxBranchesPerBeat,
    string BranchQuantumType,
    int BranchMaxThreshold,
    bool AnalysisMusicalQuality,
    double JumpProbability,
    int JumpCooldown,
    double FirstPassLinearPlaybackRatio);
