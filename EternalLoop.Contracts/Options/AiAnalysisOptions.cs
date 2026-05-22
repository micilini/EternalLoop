namespace EternalLoop.Contracts.Options;

public sealed class AiAnalysisOptions
{
    public bool IsEnabled { get; init; } = TuningDefaultValues.UseAiSimilarity;

    public string ModelId { get; init; } = AiModelDefaultValues.DiscogsEffNetModelId;

    public double RejectionThreshold { get; init; } = TuningDefaultValues.AiRejectionThreshold;

    public double PenaltyStartThreshold { get; init; } = TuningDefaultValues.AiPenaltyStartThreshold;

    public double PenaltyStrength { get; init; } = TuningDefaultValues.AiPenaltyStrength;

    public int BeatContextBefore { get; init; } = TuningDefaultValues.AiBeatContextBefore;

    public int BeatContextAfter { get; init; } = TuningDefaultValues.AiBeatContextAfter;
}
