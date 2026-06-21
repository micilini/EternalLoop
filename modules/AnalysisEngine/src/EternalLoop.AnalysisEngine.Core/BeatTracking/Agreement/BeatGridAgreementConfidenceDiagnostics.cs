using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceDiagnostics
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "not-run";

    [JsonPropertyName("legacy_candidate_id")]
    public string? LegacyCandidateId { get; init; }

    [JsonPropertyName("advisor_candidate_id")]
    public string? AdvisorCandidateId { get; init; }

    [JsonPropertyName("global_confidence")]
    public BeatGridAgreementConfidenceScore? GlobalConfidence { get; init; }

    [JsonPropertyName("high_confidence_window_count")]
    public int HighConfidenceWindowCount { get; init; }

    [JsonPropertyName("medium_or_better_window_count")]
    public int MediumOrBetterWindowCount { get; init; }

    [JsonPropertyName("window_count")]
    public int WindowCount { get; init; }

    [JsonPropertyName("high_confidence_window_ratio")]
    public double HighConfidenceWindowRatio { get; init; }

    [JsonPropertyName("future_fusion_readiness")]
    public string FutureFusionReadiness { get; init; } = "not-ready";

    [JsonPropertyName("future_fusion_ready")]
    public bool FutureFusionReady { get; init; }

    [JsonPropertyName("should_modify_final_grid")]
    public bool ShouldModifyFinalGrid { get; init; } = false;

    [JsonPropertyName("should_select_advisor")]
    public bool ShouldSelectAdvisor { get; init; } = false;

    [JsonPropertyName("should_apply_correction")]
    public bool ShouldApplyCorrection { get; init; } = false;

    [JsonPropertyName("external_benchmark_claim_status")]
    public string ExternalBenchmarkClaimStatus { get; init; } = "not-evaluated";

    [JsonPropertyName("windows")]
    public IReadOnlyList<BeatGridAgreementConfidenceWindow> Windows { get; init; } = [];

    [JsonPropertyName("unreliable_reason")]
    public string? UnreliableReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static BeatGridAgreementConfidenceDiagnostics NotAvailable(string reason)
    {
        return new BeatGridAgreementConfidenceDiagnostics
        {
            Enabled = true,
            Status = "not-available",
            UnreliableReason = reason,
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Notes = [reason]
        };
    }
}
