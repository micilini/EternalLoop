namespace EternalLoop.Contracts.Options;

public sealed class BranchFindingOptions
{
    public double SimilarityThreshold { get; init; } = TuningDefaultValues.SimilarityThreshold;

    public int LookaheadDepth { get; init; } = TuningDefaultValues.LookaheadDepth;

    public int MinJumpDistance { get; init; } = TuningDefaultValues.MinJumpDistance;

    public double TimbreWeight { get; init; } = TuningDefaultValues.TimbreWeight;

    public double PitchWeight { get; init; } = TuningDefaultValues.PitchWeight;

    public double LoudnessWeight { get; init; } = TuningDefaultValues.LoudnessWeight;

    public double BarPositionWeight { get; init; } = TuningDefaultValues.BarPositionWeight;

    public int MaxBranchesPerBeat { get; init; } = TuningDefaultValues.MaxBranchesPerBeat;

    public int LandingOffsetBeats { get; init; } = TuningDefaultValues.AiBeatContextBefore;

    public int ContinuationLookaheadDepth { get; init; } = TuningDefaultValues.PhraseValidationLookaheadDepth;

    public double ContinuationThresholdMargin { get; init; } = TuningDefaultValues.PhraseValidationThresholdMargin;

    public bool UseAiSimilarity { get; init; } = TuningDefaultValues.UseAiSimilarity;

    public double AiRejectionThreshold { get; init; } = TuningDefaultValues.AiRejectionThreshold;

    public double AiPenaltyStartThreshold { get; init; } = TuningDefaultValues.AiPenaltyStartThreshold;

    public double AiPenaltyStrength { get; init; } = TuningDefaultValues.AiPenaltyStrength;
}
