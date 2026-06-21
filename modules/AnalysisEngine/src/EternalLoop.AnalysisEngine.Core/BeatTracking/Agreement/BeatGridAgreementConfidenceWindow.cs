using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceWindow
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("start_time_seconds")]
    public double StartTimeSeconds { get; init; }

    [JsonPropertyName("end_time_seconds")]
    public double EndTimeSeconds { get; init; }

    [JsonPropertyName("legacy_beat_count")]
    public int LegacyBeatCount { get; init; }

    [JsonPropertyName("advisor_beat_count")]
    public int AdvisorBeatCount { get; init; }

    [JsonPropertyName("zero_offset_f1_70ms")]
    public double ZeroOffsetF1_70Ms { get; init; }

    [JsonPropertyName("best_offset_f1_70ms")]
    public double BestOffsetF1_70Ms { get; init; }

    [JsonPropertyName("best_offset_ms")]
    public double? BestOffsetMs { get; init; }

    [JsonPropertyName("confidence")]
    public BeatGridAgreementConfidenceScore? Confidence { get; init; }

    [JsonPropertyName("future_fusion_candidate")]
    public bool FutureFusionCandidate { get; init; }

    [JsonPropertyName("unreliable_reason")]
    public string? UnreliableReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
