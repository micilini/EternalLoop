using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceScore
{
    [JsonPropertyName("level")]
    public BeatGridAgreementConfidenceLevel Level { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("f1_70ms")]
    public double F1_70Ms { get; init; }

    [JsonPropertyName("count_ratio")]
    public double? CountRatio { get; init; }

    [JsonPropertyName("abs_offset_ms")]
    public double? AbsOffsetMs { get; init; }

    [JsonPropertyName("offset_stability_mad_ms")]
    public double? OffsetStabilityMadMs { get; init; }

    [JsonPropertyName("is_reliable")]
    public bool IsReliable { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "not-evaluated";

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
