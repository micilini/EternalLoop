using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

public sealed class BeatGridHybridSelector
{
    private readonly BeatGridHybridSelectionOptions _options;
    private readonly BeatGridHybridSafetyGate _safetyGate;

    public BeatGridHybridSelector(
        BeatGridHybridSelectionOptions? options = null,
        BeatGridHybridSafetyGate? safetyGate = null)
    {
        _options = options ?? new BeatGridHybridSelectionOptions();
        _options.Validate();
        _safetyGate = safetyGate ?? new BeatGridHybridSafetyGate(_options);
    }

    public (BeatGridCandidate Selected, BeatGridHybridSelectionDiagnostics Diagnostics) SelectExplicitHybrid(
        BeatGridCandidateSet candidateSet)
    {
        ArgumentNullException.ThrowIfNull(candidateSet);

        var legacy = candidateSet.Legacy ?? throw new InvalidOperationException("Hybrid selection requires a legacy candidate.");

        if (!_options.AllowCorrectedExperimentalAsFinal)
        {
            return (legacy, CreateDiagnostics(
                candidateSet,
                legacy,
                "disabled",
                BeatGridHybridSelectionDecision.SelectedLegacy,
                safetyPassed: false,
                safetyRejectionReason: "corrected-experimental-final-disabled",
                finalOutputSource: "legacy",
                notes: ["Corrected experimental final output disabled by options."]));
        }

        var safety = _safetyGate.Validate(candidateSet);
        if (safety.IsSafe && candidateSet.CorrectedExperimental is not null)
        {
            return (candidateSet.CorrectedExperimental, CreateDiagnostics(
                candidateSet,
                candidateSet.CorrectedExperimental,
                "selected-corrected-experimental",
                BeatGridHybridSelectionDecision.SelectedCorrectedExperimental,
                safetyPassed: true,
                safetyRejectionReason: null,
                finalOutputSource: "corrected-experimental",
                notes: safety.Notes));
        }

        return (legacy, CreateDiagnostics(
            candidateSet,
            legacy,
            "fallback-to-legacy",
            candidateSet.CorrectedExperimental is null
                ? BeatGridHybridSelectionDecision.NotAvailable
                : BeatGridHybridSelectionDecision.FallbackToLegacy,
            safetyPassed: false,
            safetyRejectionReason: safety.Reason ?? "hybrid-corrected-candidate-not-available",
            finalOutputSource: "legacy",
            notes: safety.Notes));
    }

    private BeatGridHybridSelectionDiagnostics CreateDiagnostics(
        BeatGridCandidateSet candidateSet,
        BeatGridCandidate selected,
        string status,
        BeatGridHybridSelectionDecision decision,
        bool safetyPassed,
        string? safetyRejectionReason,
        string finalOutputSource,
        IReadOnlyList<string> notes)
    {
        var fallbackCategory = ResolveFallbackCategory(candidateSet, safetyRejectionReason);

        return new BeatGridHybridSelectionDiagnostics
        {
            Enabled = true,
            CalibrationProfile = _options.CalibrationProfileName,
            Status = status,
            Decision = decision,
            SelectedCandidateId = selected.Id,
            SelectedSource = selected.Source.ToString(),
            LegacyCandidateId = candidateSet.Legacy?.Id,
            CorrectedCandidateId = candidateSet.CorrectedExperimental?.Id,
            SafetyPassed = safetyPassed,
            SafetyRejectionReason = safetyRejectionReason,
            FinalOutputSource = finalOutputSource,
            FallbackCategory = safetyPassed ? BeatGridHybridFallbackCategory.None : fallbackCategory,
            FallbackIsSafeNoop = !safetyPassed && IsSafeNoopFallback(fallbackCategory),
            FallbackIsRuntimeFailure = !safetyPassed && IsRuntimeFailure(fallbackCategory),
            WeakWindowCount = candidateSet.WeakWindows?.WeakWindowCount,
            FutureCorrectionCandidateCount = candidateSet.WeakWindows?.FutureCorrectionCandidateCount,
            CorrectionCandidateWindowCount = candidateSet.WeakWindowCorrectionPlan?.CandidateWindowCount
                ?? candidateSet.WeakWindowCorrections?.CandidateWindowCount,
            AcceptedCorrectionWindowCount = candidateSet.WeakWindowCorrections?.AcceptedWindowCount,
            RejectedCorrectionWindowCount = candidateSet.WeakWindowCorrections?.RejectedWindowCount,
            CorrectionRejectionReason = candidateSet.WeakWindowCorrections?.RejectionReason,
            TopCorrectionBlockers = candidateSet.WeakWindowCorrections?.TopBlockers ?? [],
            ExplicitOptIn = true,
            AutoUsesHybrid = false,
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Notes = notes
        };
    }

    private static BeatGridHybridFallbackCategory ResolveFallbackCategory(
        BeatGridCandidateSet candidateSet,
        string? safetyReason)
    {
        if (candidateSet.Advisor is null)
        {
            return BeatGridHybridFallbackCategory.AdvisorUnavailable;
        }

        if (candidateSet.Advisor.Quality.RejectionReason is not null
            || candidateSet.Advisor.Quality.IsDenseGrid
            || !candidateSet.Advisor.Quality.IsPlausible)
        {
            return BeatGridHybridFallbackCategory.AdvisorRejected;
        }

        if (safetyReason is null)
        {
            return BeatGridHybridFallbackCategory.Unknown;
        }

        if (candidateSet.CorrectedExperimental is null)
        {
            return ResolveMissingCorrectedCategory(candidateSet.WeakWindowCorrections, safetyReason);
        }

        if (safetyReason.Contains("dense", StringComparison.OrdinalIgnoreCase))
        {
            return BeatGridHybridFallbackCategory.CorrectedCandidateDense;
        }

        if (safetyReason.Contains("count-ratio", StringComparison.OrdinalIgnoreCase))
        {
            return BeatGridHybridFallbackCategory.CorrectedCandidateCountRatioOutOfRange;
        }

        if (safetyReason.Contains("implausible", StringComparison.OrdinalIgnoreCase)
            || safetyReason.Contains("beat-count-zero", StringComparison.OrdinalIgnoreCase)
            || safetyReason.Contains("bpm", StringComparison.OrdinalIgnoreCase)
            || safetyReason.Contains("median-interval", StringComparison.OrdinalIgnoreCase)
            || safetyReason.Contains("density", StringComparison.OrdinalIgnoreCase))
        {
            return BeatGridHybridFallbackCategory.CorrectedCandidateImplausible;
        }

        if (safetyReason.Contains("unsafe", StringComparison.OrdinalIgnoreCase)
            || safetyReason.Contains("accepted-correction-window-count-too-low", StringComparison.OrdinalIgnoreCase))
        {
            return BeatGridHybridFallbackCategory.CorrectedCandidateUnsafe;
        }

        return BeatGridHybridFallbackCategory.Unknown;
    }

    private static BeatGridHybridFallbackCategory ResolveMissingCorrectedCategory(
        BeatGridWeakWindowCorrectionDiagnostics? corrections,
        string safetyReason)
    {
        if (corrections is null)
        {
            return BeatGridHybridFallbackCategory.CorrectionDiagnosticsMissing;
        }

        if (string.Equals(corrections.RejectionReason, "no-correction-windows-accepted", StringComparison.OrdinalIgnoreCase))
        {
            return BeatGridHybridFallbackCategory.NoCorrectionWindowsAccepted;
        }

        if (string.Equals(corrections.RejectionReason, "corrected-experimental-candidate-missing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(safetyReason, "corrected-experimental-candidate-missing", StringComparison.OrdinalIgnoreCase))
        {
            return BeatGridHybridFallbackCategory.CorrectedCandidateMissing;
        }

        if (string.Equals(corrections.Status, "not-available", StringComparison.OrdinalIgnoreCase))
        {
            return corrections.RejectionReason is null
                ? BeatGridHybridFallbackCategory.CorrectionDiagnosticsMissing
                : BeatGridHybridFallbackCategory.WeakWindowsNotReady;
        }

        if (!corrections.CorrectedCandidateCreated)
        {
            return BeatGridHybridFallbackCategory.CorrectedCandidateNotCreated;
        }

        return BeatGridHybridFallbackCategory.CorrectedCandidateMissing;
    }

    private static bool IsSafeNoopFallback(BeatGridHybridFallbackCategory category)
    {
        return category is
            BeatGridHybridFallbackCategory.CorrectedCandidateMissing
            or BeatGridHybridFallbackCategory.CorrectedCandidateNotCreated
            or BeatGridHybridFallbackCategory.NoCorrectionWindowsAccepted
            or BeatGridHybridFallbackCategory.WeakWindowsNotReady
            or BeatGridHybridFallbackCategory.CorrectedCandidateUnsafe
            or BeatGridHybridFallbackCategory.CorrectedCandidateDense
            or BeatGridHybridFallbackCategory.CorrectedCandidateImplausible
            or BeatGridHybridFallbackCategory.CorrectedCandidateCountRatioOutOfRange;
    }

    private static bool IsRuntimeFailure(BeatGridHybridFallbackCategory category)
    {
        return category is
            BeatGridHybridFallbackCategory.RuntimeFailure
            or BeatGridHybridFallbackCategory.AdvisorUnavailable
            or BeatGridHybridFallbackCategory.AdvisorRejected
            or BeatGridHybridFallbackCategory.CorrectionDiagnosticsMissing;
    }
}
