using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentDiagnostics
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "not-run";

    [JsonPropertyName("reference_candidate_id")]
    public string? ReferenceCandidateId { get; init; }

    [JsonPropertyName("candidate_id")]
    public string? CandidateId { get; init; }

    [JsonPropertyName("reference_beat_count")]
    public int ReferenceBeatCount { get; init; }

    [JsonPropertyName("candidate_beat_count")]
    public int CandidateBeatCount { get; init; }

    [JsonPropertyName("count_ratio")]
    public double? CountRatio { get; init; }

    [JsonPropertyName("zero_offset")]
    public BeatGridPhaseAlignmentMetrics? ZeroOffset { get; init; }

    [JsonPropertyName("best_offset_ms")]
    public double? BestOffsetMs { get; init; }

    [JsonPropertyName("best_offset")]
    public BeatGridPhaseAlignmentMetrics? BestOffset { get; init; }

    [JsonPropertyName("improvement_f1_70ms")]
    public double? ImprovementF1_70Ms { get; init; }

    [JsonPropertyName("offset_direction")]
    public string OffsetDirection { get; init; } = "none";

    [JsonPropertyName("offset_stability_mad_ms")]
    public double? OffsetStabilityMadMs { get; init; }

    [JsonPropertyName("is_offset_stable")]
    public bool IsOffsetStable { get; init; }

    [JsonPropertyName("confidence")]
    public BeatGridPhaseAlignmentConfidence Confidence { get; init; } = BeatGridPhaseAlignmentConfidence.None;

    [JsonPropertyName("should_apply_correction")]
    public bool ShouldApplyCorrection { get; init; } = false;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; init; } = "diagnostic-only-do-not-correct";

    [JsonPropertyName("windows")]
    public IReadOnlyList<BeatGridPhaseAlignmentWindow> Windows { get; init; } = [];

    [JsonPropertyName("unreliable_reason")]
    public string? UnreliableReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static BeatGridPhaseAlignmentDiagnostics NotAvailable(string reason)
    {
        return new BeatGridPhaseAlignmentDiagnostics
        {
            Enabled = true,
            Status = "not-available",
            UnreliableReason = reason,
            Notes = [reason]
        };
    }
}
