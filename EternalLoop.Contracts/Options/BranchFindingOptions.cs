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

    public int LandingOffsetBeats { get; init; } = 1;

    public int ContinuationLookaheadDepth { get; init; } = TuningDefaultValues.PhraseValidationLookaheadDepth;

    public double ContinuationThresholdMargin { get; init; } = TuningDefaultValues.PhraseValidationThresholdMargin;
}
