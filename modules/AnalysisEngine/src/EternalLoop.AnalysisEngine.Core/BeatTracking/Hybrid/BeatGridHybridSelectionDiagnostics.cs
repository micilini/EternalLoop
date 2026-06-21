using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

public sealed class BeatGridHybridSelectionDiagnostics
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("provider")]
    public string Provider { get; init; } = "hybrid";

    [JsonPropertyName("calibration_profile")]
    public string CalibrationProfile { get; init; } = "strict-production";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "not-run";

    [JsonPropertyName("decision")]
    public BeatGridHybridSelectionDecision Decision { get; init; }

    [JsonPropertyName("selected_candidate_id")]
    public string? SelectedCandidateId { get; init; }

    [JsonPropertyName("selected_source")]
    public string? SelectedSource { get; init; }

    [JsonPropertyName("legacy_candidate_id")]
    public string? LegacyCandidateId { get; init; }

    [JsonPropertyName("corrected_candidate_id")]
    public string? CorrectedCandidateId { get; init; }

    [JsonPropertyName("safety_passed")]
    public bool SafetyPassed { get; init; }

    [JsonPropertyName("safety_rejection_reason")]
    public string? SafetyRejectionReason { get; init; }

    [JsonPropertyName("final_output_source")]
    public string FinalOutputSource { get; init; } = "legacy";

    [JsonPropertyName("fallback_category")]
    public BeatGridHybridFallbackCategory FallbackCategory { get; init; }

    [JsonPropertyName("fallback_is_safe_noop")]
    public bool FallbackIsSafeNoop { get; init; }

    [JsonPropertyName("fallback_is_runtime_failure")]
    public bool FallbackIsRuntimeFailure { get; init; }

    [JsonPropertyName("weak_window_count")]
    public int? WeakWindowCount { get; init; }

    [JsonPropertyName("future_correction_candidate_count")]
    public int? FutureCorrectionCandidateCount { get; init; }

    [JsonPropertyName("correction_candidate_window_count")]
    public int? CorrectionCandidateWindowCount { get; init; }

    [JsonPropertyName("accepted_correction_window_count")]
    public int? AcceptedCorrectionWindowCount { get; init; }

    [JsonPropertyName("rejected_correction_window_count")]
    public int? RejectedCorrectionWindowCount { get; init; }

    [JsonPropertyName("correction_rejection_reason")]
    public string? CorrectionRejectionReason { get; init; }

    [JsonPropertyName("top_correction_blockers")]
    public IReadOnlyList<string> TopCorrectionBlockers { get; init; } = [];

    [JsonPropertyName("explicit_opt_in")]
    public bool ExplicitOptIn { get; init; }

    [JsonPropertyName("auto_uses_hybrid")]
    public bool AutoUsesHybrid { get; init; } = false;

    [JsonPropertyName("external_benchmark_claim_status")]
    public string ExternalBenchmarkClaimStatus { get; init; } = "not-evaluated";

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
