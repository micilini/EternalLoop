using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

public sealed class BeatGridHybridSafetyGate
{
    private readonly BeatGridHybridSelectionOptions _options;

    public BeatGridHybridSafetyGate(BeatGridHybridSelectionOptions? options = null)
    {
        _options = options ?? new BeatGridHybridSelectionOptions();
        _options.Validate();
    }

    public BeatGridHybridSafetyResult Validate(BeatGridCandidateSet? candidateSet)
    {
        if (candidateSet is null) return BeatGridHybridSafetyResult.Unsafe("candidate-set-missing");
        if (candidateSet.Legacy is null) return BeatGridHybridSafetyResult.Unsafe("legacy-candidate-missing");
        if (candidateSet.CorrectedExperimental is null) return BeatGridHybridSafetyResult.Unsafe("corrected-experimental-candidate-missing");
        if (_options.RequireCorrectionDiagnostics && candidateSet.WeakWindowCorrections is null) return BeatGridHybridSafetyResult.Unsafe("weak-window-corrections-missing");
        if (_options.RequireCorrectionDiagnostics && candidateSet.WeakWindowCorrectionPlan is null) return BeatGridHybridSafetyResult.Unsafe("weak-window-correction-plan-missing");

        var legacy = candidateSet.Legacy;
        var corrected = candidateSet.CorrectedExperimental;
        var corrections = candidateSet.WeakWindowCorrections;
        var plan = candidateSet.WeakWindowCorrectionPlan;

        if (corrections is not null)
        {
            if (!corrections.CorrectedCandidateCreated) return BeatGridHybridSafetyResult.Unsafe("corrected-candidate-not-created");
            if (corrections.AcceptedWindowCount < _options.MinAcceptedCorrectionWindows) return BeatGridHybridSafetyResult.Unsafe("accepted-correction-window-count-too-low");
            if (corrections.ShouldModifyFinalGrid) return BeatGridHybridSafetyResult.Unsafe("hbg07-should-modify-final-grid-unsafe");
            if (corrections.ShouldApplyCorrection) return BeatGridHybridSafetyResult.Unsafe("hbg07-should-apply-correction-unsafe");
            if (_options.RequireMadmomClaimNotEvaluated
                && !string.Equals(corrections.ExternalBenchmarkClaimStatus, "not-evaluated", StringComparison.OrdinalIgnoreCase))
            {
                return BeatGridHybridSafetyResult.Unsafe("external-benchmark-claim-status-not-evaluated");
            }
        }

        if (plan is not null && plan.AcceptedWindowCount < _options.MinAcceptedCorrectionWindows)
        {
            return BeatGridHybridSafetyResult.Unsafe("plan-accepted-correction-window-count-too-low");
        }

        if (_options.RequireCorrectedCandidateNotDense && corrected.Quality.IsDenseGrid) return BeatGridHybridSafetyResult.Unsafe("corrected-candidate-dense-grid");
        if (!corrected.Quality.IsPlausible) return BeatGridHybridSafetyResult.Unsafe(corrected.Quality.RejectionReason ?? "corrected-candidate-implausible");
        if (corrected.EstimatedBpm > _options.MaxCorrectedBpm || corrected.Quality.EstimatedBpm > _options.MaxCorrectedBpm) return BeatGridHybridSafetyResult.Unsafe("corrected-bpm-too-high");
        if (corrected.Quality.MedianIntervalSeconds < _options.MinCorrectedMedianIntervalSeconds) return BeatGridHybridSafetyResult.Unsafe("corrected-median-interval-too-low");
        if (corrected.Quality.BeatDensityPerSecond > _options.MaxCorrectedBeatDensityPerSecond) return BeatGridHybridSafetyResult.Unsafe("corrected-beat-density-too-high");
        if (legacy.BeatTimes.Length == 0) return BeatGridHybridSafetyResult.Unsafe("legacy-beat-count-zero");

        var ratioDelta = Math.Abs(corrected.BeatTimes.Length / (double)legacy.BeatTimes.Length - 1.0);
        if (ratioDelta > _options.MaxCorrectedVsLegacyCountRatioDelta) return BeatGridHybridSafetyResult.Unsafe("corrected-vs-legacy-count-ratio-out-of-range");

        return BeatGridHybridSafetyResult.Safe(["Explicit hybrid guardrails passed."]);
    }
}
