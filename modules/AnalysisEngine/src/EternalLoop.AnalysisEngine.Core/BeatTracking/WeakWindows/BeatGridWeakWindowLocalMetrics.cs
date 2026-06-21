using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindowLocalMetrics
{
    [JsonPropertyName("legacy_interval_cv")]
    public double? LegacyIntervalCv { get; init; }

    [JsonPropertyName("advisor_interval_cv")]
    public double? AdvisorIntervalCv { get; init; }

    [JsonPropertyName("legacy_median_interval_seconds")]
    public double? LegacyMedianIntervalSeconds { get; init; }

    [JsonPropertyName("advisor_median_interval_seconds")]
    public double? AdvisorMedianIntervalSeconds { get; init; }

    [JsonPropertyName("legacy_beat_density_per_second")]
    public double? LegacyBeatDensityPerSecond { get; init; }

    [JsonPropertyName("advisor_beat_density_per_second")]
    public double? AdvisorBeatDensityPerSecond { get; init; }

    [JsonPropertyName("local_count_ratio")]
    public double? LocalCountRatio { get; init; }

    [JsonPropertyName("local_best_offset_ms")]
    public double? LocalBestOffsetMs { get; init; }

    [JsonPropertyName("local_zero_offset_f1_70ms")]
    public double? LocalZeroOffsetF1_70Ms { get; init; }

    [JsonPropertyName("local_best_offset_f1_70ms")]
    public double? LocalBestOffsetF1_70Ms { get; init; }

    [JsonPropertyName("agreement_confidence_score")]
    public double? AgreementConfidenceScore { get; init; }

    [JsonPropertyName("weakness_score")]
    public double WeaknessScore { get; init; }

    [JsonPropertyName("advisor_strength_score")]
    public double AdvisorStrengthScore { get; init; }

    [JsonPropertyName("correction_readiness_score")]
    public double CorrectionReadinessScore { get; init; }
}
