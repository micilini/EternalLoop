using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentMetrics
{
    [JsonPropertyName("precision_50ms")]
    public double Precision50Ms { get; init; }

    [JsonPropertyName("recall_50ms")]
    public double Recall50Ms { get; init; }

    [JsonPropertyName("f1_50ms")]
    public double F1_50Ms { get; init; }

    [JsonPropertyName("precision_70ms")]
    public double Precision70Ms { get; init; }

    [JsonPropertyName("recall_70ms")]
    public double Recall70Ms { get; init; }

    [JsonPropertyName("f1_70ms")]
    public double F1_70Ms { get; init; }

    [JsonPropertyName("precision_100ms")]
    public double Precision100Ms { get; init; }

    [JsonPropertyName("recall_100ms")]
    public double Recall100Ms { get; init; }

    [JsonPropertyName("f1_100ms")]
    public double F1_100Ms { get; init; }

    [JsonPropertyName("matched_count_70ms")]
    public int MatchedCount70Ms { get; init; }

    [JsonPropertyName("mean_abs_error_ms")]
    public double? MeanAbsErrorMs { get; init; }

    [JsonPropertyName("median_abs_error_ms")]
    public double? MedianAbsErrorMs { get; init; }

    [JsonPropertyName("mean_signed_error_ms")]
    public double? MeanSignedErrorMs { get; init; }
}
