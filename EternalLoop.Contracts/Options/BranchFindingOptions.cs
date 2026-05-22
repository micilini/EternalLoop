using EternalLoop.Contracts.Enums;

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

    public bool UseDurationSimilarityGate { get; init; } = TuningDefaultValues.UseDurationSimilarityGate;

    public double DurationPenaltyStartRatio { get; init; } = TuningDefaultValues.DurationPenaltyStartRatio;

    public double DurationRejectionRatio { get; init; } = TuningDefaultValues.DurationRejectionRatio;

    public double DurationPenaltyStrength { get; init; } = TuningDefaultValues.DurationPenaltyStrength;

    public bool UseConfidencePenalty { get; init; } = TuningDefaultValues.UseConfidencePenalty;

    public double ConfidencePenaltyStart { get; init; } = TuningDefaultValues.ConfidencePenaltyStart;

    public double ConfidenceRejectionThreshold { get; init; } = TuningDefaultValues.ConfidenceRejectionThreshold;

    public double ConfidencePenaltyStrength { get; init; } = TuningDefaultValues.ConfidencePenaltyStrength;

    public MetricPositionMode MetricPositionMode { get; init; } = TuningDefaultValues.DefaultMetricPositionMode;

    public double MetricPositionPenaltyStrength { get; init; } = TuningDefaultValues.MetricPositionPenaltyStrength;

    public double MetricPositionRejectionThreshold { get; init; } = TuningDefaultValues.MetricPositionRejectionThreshold;

    public double TargetBranchSourceRatio { get; init; } = TuningDefaultValues.TargetBranchSourceRatio;

    public double MaxBranchSourceRatio { get; init; } = TuningDefaultValues.MaxBranchSourceRatio;

    public bool UseMicrosegmentSimilarity { get; init; } = TuningDefaultValues.UseMicrosegmentSimilarity;

    public int MicrosegmentCount { get; init; } = TuningDefaultValues.MicrosegmentCount;

    public double MicrosegmentPenaltyStartThreshold { get; init; } = TuningDefaultValues.MicrosegmentPenaltyStartThreshold;

    public double MicrosegmentRejectionThreshold { get; init; } = TuningDefaultValues.MicrosegmentRejectionThreshold;

    public double MicrosegmentPenaltyStrength { get; init; } = TuningDefaultValues.MicrosegmentPenaltyStrength;
}
