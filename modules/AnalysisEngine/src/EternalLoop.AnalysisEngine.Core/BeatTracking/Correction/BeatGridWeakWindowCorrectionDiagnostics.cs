using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionDiagnostics
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "not-run";

    [JsonPropertyName("calibration_profile")]
    public string CalibrationProfile { get; init; } = "strict-production";

    [JsonPropertyName("mode")]
    public BeatGridWeakWindowCorrectionMode Mode { get; init; }

    [JsonPropertyName("corrected_candidate_created")]
    public bool CorrectedCandidateCreated { get; init; }

    [JsonPropertyName("corrected_candidate_id")]
    public string? CorrectedCandidateId { get; init; }

    [JsonPropertyName("accepted_window_count")]
    public int AcceptedWindowCount { get; init; }

    [JsonPropertyName("rejected_window_count")]
    public int RejectedWindowCount { get; init; }

    [JsonPropertyName("weak_window_count")]
    public int WeakWindowCount { get; init; }

    [JsonPropertyName("future_correction_candidate_count")]
    public int FutureCorrectionCandidateCount { get; init; }

    [JsonPropertyName("candidate_window_count")]
    public int CandidateWindowCount { get; init; }

    [JsonPropertyName("blocker_counts")]
    public IReadOnlyDictionary<string, int> BlockerCounts { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("top_blockers")]
    public IReadOnlyList<string> TopBlockers { get; init; } = [];

    [JsonPropertyName("diagnostic_window_count")]
    public int DiagnosticWindowCount { get; init; }

    [JsonPropertyName("legacy_beat_count")]
    public int LegacyBeatCount { get; init; }

    [JsonPropertyName("corrected_beat_count")]
    public int CorrectedBeatCount { get; init; }

    [JsonPropertyName("advisor_beat_count")]
    public int AdvisorBeatCount { get; init; }

    [JsonPropertyName("corrected_vs_legacy_count_delta")]
    public int CorrectedVsLegacyCountDelta { get; init; }

    [JsonPropertyName("corrected_estimated_bpm")]
    public double? CorrectedEstimatedBpm { get; init; }

    [JsonPropertyName("corrected_is_dense_grid")]
    public bool CorrectedIsDenseGrid { get; init; }

    [JsonPropertyName("should_modify_final_grid")]
    public bool ShouldModifyFinalGrid { get; init; } = false;

    [JsonPropertyName("should_select_corrected_candidate")]
    public bool ShouldSelectCorrectedCandidate { get; init; } = false;

    [JsonPropertyName("should_apply_correction")]
    public bool ShouldApplyCorrection { get; init; } = false;

    [JsonPropertyName("external_benchmark_claim_status")]
    public string ExternalBenchmarkClaimStatus { get; init; } = "not-evaluated";

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static BeatGridWeakWindowCorrectionDiagnostics NotAvailable(string reason)
    {
        return new BeatGridWeakWindowCorrectionDiagnostics
        {
            Enabled = true,
            Status = "not-available",
            Mode = BeatGridWeakWindowCorrectionMode.DiagnosticsOnly,
            RejectionReason = reason,
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Notes = [reason]
        };
    }
}
