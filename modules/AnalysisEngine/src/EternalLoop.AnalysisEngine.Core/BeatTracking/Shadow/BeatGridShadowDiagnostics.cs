using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;

public sealed class BeatGridShadowDiagnostics
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "off";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "not-run";

    [JsonPropertyName("advisor_provider")]
    public string AdvisorProvider { get; init; } = "beat-this";

    [JsonPropertyName("legacy_provider")]
    public string LegacyProvider { get; init; } = "built-in";

    [JsonPropertyName("legacy_beat_count")]
    public int LegacyBeatCount { get; init; }

    [JsonPropertyName("advisor_beat_count")]
    public int AdvisorBeatCount { get; init; }

    [JsonPropertyName("legacy_bpm")]
    public double? LegacyBpm { get; init; }

    [JsonPropertyName("advisor_bpm")]
    public double? AdvisorBpm { get; init; }

    [JsonPropertyName("count_ratio")]
    public double? CountRatio { get; init; }

    [JsonPropertyName("bpm_delta")]
    public double? BpmDelta { get; init; }

    [JsonPropertyName("agreement_precision_50ms")]
    public double? AgreementPrecision50Ms { get; init; }

    [JsonPropertyName("agreement_recall_50ms")]
    public double? AgreementRecall50Ms { get; init; }

    [JsonPropertyName("agreement_f1_50ms")]
    public double? AgreementF1_50Ms { get; init; }

    [JsonPropertyName("agreement_precision_70ms")]
    public double? AgreementPrecision70Ms { get; init; }

    [JsonPropertyName("agreement_recall_70ms")]
    public double? AgreementRecall70Ms { get; init; }

    [JsonPropertyName("agreement_f1_70ms")]
    public double? AgreementF1_70Ms { get; init; }

    [JsonPropertyName("agreement_precision_100ms")]
    public double? AgreementPrecision100Ms { get; init; }

    [JsonPropertyName("agreement_recall_100ms")]
    public double? AgreementRecall100Ms { get; init; }

    [JsonPropertyName("agreement_f1_100ms")]
    public double? AgreementF1_100Ms { get; init; }

    [JsonPropertyName("best_offset_ms")]
    public double? BestOffsetMs { get; init; }

    [JsonPropertyName("best_offset_f1_70ms")]
    public double? BestOffsetF1_70Ms { get; init; }

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; init; }

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];

    [JsonPropertyName("candidate_set_enabled")]
    public bool CandidateSetEnabled { get; init; }

    [JsonPropertyName("selected_candidate_id")]
    public string? SelectedCandidateId { get; init; }

    [JsonPropertyName("advisor_candidate_id")]
    public string? AdvisorCandidateId { get; init; }

    [JsonPropertyName("phase_alignment_status")]
    public string? PhaseAlignmentStatus { get; init; }

    [JsonPropertyName("phase_alignment_best_offset_ms")]
    public double? PhaseAlignmentBestOffsetMs { get; init; }

    [JsonPropertyName("phase_alignment_confidence")]
    public string? PhaseAlignmentConfidence { get; init; }

    [JsonPropertyName("phase_alignment_should_apply_correction")]
    public bool PhaseAlignmentShouldApplyCorrection { get; init; }

    [JsonPropertyName("agreement_confidence_status")]
    public string? AgreementConfidenceStatus { get; init; }

    [JsonPropertyName("agreement_confidence_level")]
    public string? AgreementConfidenceLevel { get; init; }

    [JsonPropertyName("agreement_confidence_score")]
    public double? AgreementConfidenceScore { get; init; }

    [JsonPropertyName("future_fusion_readiness")]
    public string? FutureFusionReadiness { get; init; }

    [JsonPropertyName("future_fusion_ready")]
    public bool FutureFusionReady { get; init; }

    [JsonPropertyName("agreement_should_modify_final_grid")]
    public bool AgreementShouldModifyFinalGrid { get; init; }

    [JsonPropertyName("external_benchmark_claim_status")]
    public string? ExternalBenchmarkClaimStatus { get; init; }

    [JsonPropertyName("weak_window_status")]
    public string? WeakWindowStatus { get; init; }

    [JsonPropertyName("weak_window_count")]
    public int WeakWindowCount { get; init; }

    [JsonPropertyName("future_correction_candidate_count")]
    public int FutureCorrectionCandidateCount { get; init; }

    [JsonPropertyName("future_correction_readiness")]
    public string? FutureCorrectionReadiness { get; init; }

    [JsonPropertyName("future_correction_ready")]
    public bool FutureCorrectionReady { get; init; }

    [JsonPropertyName("weak_windows_should_modify_final_grid")]
    public bool WeakWindowsShouldModifyFinalGrid { get; init; }

    [JsonPropertyName("weak_windows_should_apply_correction")]
    public bool WeakWindowsShouldApplyCorrection { get; init; }

    [JsonPropertyName("weak_window_correction_status")]
    public string? WeakWindowCorrectionStatus { get; init; }

    [JsonPropertyName("corrected_candidate_created")]
    public bool CorrectedCandidateCreated { get; init; }

    [JsonPropertyName("corrected_candidate_id")]
    public string? CorrectedCandidateId { get; init; }

    [JsonPropertyName("correction_accepted_window_count")]
    public int CorrectionAcceptedWindowCount { get; init; }

    [JsonPropertyName("correction_rejected_window_count")]
    public int CorrectionRejectedWindowCount { get; init; }

    [JsonPropertyName("correction_should_modify_final_grid")]
    public bool CorrectionShouldModifyFinalGrid { get; init; }

    [JsonPropertyName("correction_should_select_corrected_candidate")]
    public bool CorrectionShouldSelectCorrectedCandidate { get; init; }

    [JsonPropertyName("correction_should_apply_correction")]
    public bool CorrectionShouldApplyCorrection { get; init; }

    public static BeatGridShadowDiagnostics NotConfigured(
        BeatTrackingResult legacy,
        BeatGridCandidateSet? candidateSet = null)
    {
        return new BeatGridShadowDiagnostics
        {
            Enabled = true,
            Mode = "shadow",
            Status = "not-configured",
            LegacyBeatCount = legacy.BeatTimes.Length,
            LegacyBpm = legacy.EstimatedBpm,
            Notes = ["Beat This advisor tracker is not configured."],
            CandidateSetEnabled = candidateSet?.Diagnostics.Enabled == true,
            SelectedCandidateId = candidateSet?.Diagnostics.SelectedCandidateId,
            AdvisorCandidateId = candidateSet?.Diagnostics.AdvisorCandidateId,
            PhaseAlignmentStatus = candidateSet?.PhaseAlignment?.Status,
            PhaseAlignmentBestOffsetMs = candidateSet?.PhaseAlignment?.BestOffsetMs,
            PhaseAlignmentConfidence = candidateSet?.PhaseAlignment?.Confidence.ToString(),
            PhaseAlignmentShouldApplyCorrection = false,
            AgreementConfidenceStatus = candidateSet?.AgreementConfidence?.Status,
            AgreementConfidenceLevel = candidateSet?.AgreementConfidence?.GlobalConfidence?.Level.ToString(),
            AgreementConfidenceScore = candidateSet?.AgreementConfidence?.GlobalConfidence?.Score,
            FutureFusionReadiness = candidateSet?.AgreementConfidence?.FutureFusionReadiness,
            FutureFusionReady = candidateSet?.AgreementConfidence?.FutureFusionReady == true,
            AgreementShouldModifyFinalGrid = false,
            ExternalBenchmarkClaimStatus = candidateSet?.AgreementConfidence?.ExternalBenchmarkClaimStatus ?? "not-evaluated",
            WeakWindowStatus = candidateSet?.WeakWindows?.Status,
            WeakWindowCount = candidateSet?.WeakWindows?.WeakWindowCount ?? 0,
            FutureCorrectionCandidateCount = candidateSet?.WeakWindows?.FutureCorrectionCandidateCount ?? 0,
            FutureCorrectionReadiness = candidateSet?.WeakWindows?.FutureCorrectionReadiness,
            FutureCorrectionReady = candidateSet?.WeakWindows?.FutureCorrectionReady == true,
            WeakWindowsShouldModifyFinalGrid = false,
            WeakWindowsShouldApplyCorrection = false,
            WeakWindowCorrectionStatus = candidateSet?.WeakWindowCorrections?.Status,
            CorrectedCandidateCreated = candidateSet?.WeakWindowCorrections?.CorrectedCandidateCreated == true,
            CorrectedCandidateId = candidateSet?.WeakWindowCorrections?.CorrectedCandidateId,
            CorrectionAcceptedWindowCount = candidateSet?.WeakWindowCorrections?.AcceptedWindowCount ?? 0,
            CorrectionRejectedWindowCount = candidateSet?.WeakWindowCorrections?.RejectedWindowCount ?? 0,
            CorrectionShouldModifyFinalGrid = false,
            CorrectionShouldSelectCorrectedCandidate = false,
            CorrectionShouldApplyCorrection = false
        };
    }

    public static BeatGridShadowDiagnostics Failed(
        BeatTrackingResult legacy,
        string failureReason,
        BeatGridCandidateSet? candidateSet = null)
    {
        return new BeatGridShadowDiagnostics
        {
            Enabled = true,
            Mode = "shadow",
            Status = "failed",
            LegacyBeatCount = legacy.BeatTimes.Length,
            LegacyBpm = legacy.EstimatedBpm,
            FailureReason = failureReason,
            CandidateSetEnabled = candidateSet?.Diagnostics.Enabled == true,
            SelectedCandidateId = candidateSet?.Diagnostics.SelectedCandidateId,
            AdvisorCandidateId = candidateSet?.Diagnostics.AdvisorCandidateId,
            PhaseAlignmentStatus = candidateSet?.PhaseAlignment?.Status,
            PhaseAlignmentBestOffsetMs = candidateSet?.PhaseAlignment?.BestOffsetMs,
            PhaseAlignmentConfidence = candidateSet?.PhaseAlignment?.Confidence.ToString(),
            PhaseAlignmentShouldApplyCorrection = false,
            AgreementConfidenceStatus = candidateSet?.AgreementConfidence?.Status,
            AgreementConfidenceLevel = candidateSet?.AgreementConfidence?.GlobalConfidence?.Level.ToString(),
            AgreementConfidenceScore = candidateSet?.AgreementConfidence?.GlobalConfidence?.Score,
            FutureFusionReadiness = candidateSet?.AgreementConfidence?.FutureFusionReadiness,
            FutureFusionReady = candidateSet?.AgreementConfidence?.FutureFusionReady == true,
            AgreementShouldModifyFinalGrid = false,
            ExternalBenchmarkClaimStatus = candidateSet?.AgreementConfidence?.ExternalBenchmarkClaimStatus ?? "not-evaluated",
            WeakWindowStatus = candidateSet?.WeakWindows?.Status,
            WeakWindowCount = candidateSet?.WeakWindows?.WeakWindowCount ?? 0,
            FutureCorrectionCandidateCount = candidateSet?.WeakWindows?.FutureCorrectionCandidateCount ?? 0,
            FutureCorrectionReadiness = candidateSet?.WeakWindows?.FutureCorrectionReadiness,
            FutureCorrectionReady = candidateSet?.WeakWindows?.FutureCorrectionReady == true,
            WeakWindowsShouldModifyFinalGrid = false,
            WeakWindowsShouldApplyCorrection = false,
            WeakWindowCorrectionStatus = candidateSet?.WeakWindowCorrections?.Status,
            CorrectedCandidateCreated = candidateSet?.WeakWindowCorrections?.CorrectedCandidateCreated == true,
            CorrectedCandidateId = candidateSet?.WeakWindowCorrections?.CorrectedCandidateId,
            CorrectionAcceptedWindowCount = candidateSet?.WeakWindowCorrections?.AcceptedWindowCount ?? 0,
            CorrectionRejectedWindowCount = candidateSet?.WeakWindowCorrections?.RejectedWindowCount ?? 0,
            CorrectionShouldModifyFinalGrid = false,
            CorrectionShouldSelectCorrectedCandidate = false,
            CorrectionShouldApplyCorrection = false
        };
    }

    public static BeatGridShadowDiagnostics Rejected(
        BeatTrackingResult legacy,
        BeatTrackingResult advisor,
        string rejectionReason,
        BeatGridShadowComparison comparison,
        BeatGridCandidateSet? candidateSet = null)
    {
        return FromComparison(legacy, advisor, comparison, "rejected", rejectionReason, candidateSet);
    }

    public static BeatGridShadowDiagnostics Succeeded(
        BeatTrackingResult legacy,
        BeatTrackingResult advisor,
        BeatGridShadowComparison comparison,
        BeatGridCandidateSet? candidateSet = null)
    {
        return FromComparison(legacy, advisor, comparison, "succeeded", null, candidateSet);
    }

    private static BeatGridShadowDiagnostics FromComparison(
        BeatTrackingResult legacy,
        BeatTrackingResult advisor,
        BeatGridShadowComparison comparison,
        string status,
        string? rejectionReason,
        BeatGridCandidateSet? candidateSet)
    {
        return new BeatGridShadowDiagnostics
        {
            Enabled = true,
            Mode = "shadow",
            Status = status,
            LegacyProvider = legacy.ProviderName,
            AdvisorProvider = advisor.ProviderName,
            LegacyBeatCount = legacy.BeatTimes.Length,
            AdvisorBeatCount = advisor.BeatTimes.Length,
            LegacyBpm = legacy.EstimatedBpm,
            AdvisorBpm = advisor.EstimatedBpm,
            CountRatio = comparison.CountRatio,
            BpmDelta = comparison.BpmDelta,
            AgreementPrecision50Ms = comparison.Precision50Ms,
            AgreementRecall50Ms = comparison.Recall50Ms,
            AgreementF1_50Ms = comparison.F1_50Ms,
            AgreementPrecision70Ms = comparison.Precision70Ms,
            AgreementRecall70Ms = comparison.Recall70Ms,
            AgreementF1_70Ms = comparison.F1_70Ms,
            AgreementPrecision100Ms = comparison.Precision100Ms,
            AgreementRecall100Ms = comparison.Recall100Ms,
            AgreementF1_100Ms = comparison.F1_100Ms,
            BestOffsetMs = comparison.BestOffsetMs,
            BestOffsetF1_70Ms = comparison.BestOffsetF1_70Ms,
            RejectionReason = rejectionReason,
            CandidateSetEnabled = candidateSet?.Diagnostics.Enabled == true,
            SelectedCandidateId = candidateSet?.Diagnostics.SelectedCandidateId,
            AdvisorCandidateId = candidateSet?.Diagnostics.AdvisorCandidateId,
            PhaseAlignmentStatus = candidateSet?.PhaseAlignment?.Status,
            PhaseAlignmentBestOffsetMs = candidateSet?.PhaseAlignment?.BestOffsetMs,
            PhaseAlignmentConfidence = candidateSet?.PhaseAlignment?.Confidence.ToString(),
            PhaseAlignmentShouldApplyCorrection = false,
            AgreementConfidenceStatus = candidateSet?.AgreementConfidence?.Status,
            AgreementConfidenceLevel = candidateSet?.AgreementConfidence?.GlobalConfidence?.Level.ToString(),
            AgreementConfidenceScore = candidateSet?.AgreementConfidence?.GlobalConfidence?.Score,
            FutureFusionReadiness = candidateSet?.AgreementConfidence?.FutureFusionReadiness,
            FutureFusionReady = candidateSet?.AgreementConfidence?.FutureFusionReady == true,
            AgreementShouldModifyFinalGrid = false,
            ExternalBenchmarkClaimStatus = candidateSet?.AgreementConfidence?.ExternalBenchmarkClaimStatus ?? "not-evaluated",
            WeakWindowStatus = candidateSet?.WeakWindows?.Status,
            WeakWindowCount = candidateSet?.WeakWindows?.WeakWindowCount ?? 0,
            FutureCorrectionCandidateCount = candidateSet?.WeakWindows?.FutureCorrectionCandidateCount ?? 0,
            FutureCorrectionReadiness = candidateSet?.WeakWindows?.FutureCorrectionReadiness,
            FutureCorrectionReady = candidateSet?.WeakWindows?.FutureCorrectionReady == true,
            WeakWindowsShouldModifyFinalGrid = false,
            WeakWindowsShouldApplyCorrection = false,
            WeakWindowCorrectionStatus = candidateSet?.WeakWindowCorrections?.Status,
            CorrectedCandidateCreated = candidateSet?.WeakWindowCorrections?.CorrectedCandidateCreated == true,
            CorrectedCandidateId = candidateSet?.WeakWindowCorrections?.CorrectedCandidateId,
            CorrectionAcceptedWindowCount = candidateSet?.WeakWindowCorrections?.AcceptedWindowCount ?? 0,
            CorrectionRejectedWindowCount = candidateSet?.WeakWindowCorrections?.RejectedWindowCount ?? 0,
            CorrectionShouldModifyFinalGrid = false,
            CorrectionShouldSelectCorrectedCandidate = false,
            CorrectionShouldApplyCorrection = false
        };
    }
}
