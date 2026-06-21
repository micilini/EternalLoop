using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

public sealed class BeatGridCandidateSet
{
    [JsonPropertyName("legacy")]
    public BeatGridCandidate? Legacy { get; init; }

    [JsonPropertyName("advisor")]
    public BeatGridCandidate? Advisor { get; init; }

    [JsonPropertyName("selected")]
    public BeatGridCandidate? Selected { get; init; }

    [JsonPropertyName("corrected_experimental")]
    public BeatGridCandidate? CorrectedExperimental { get; init; }

    [JsonPropertyName("all")]
    public IReadOnlyList<BeatGridCandidate> All { get; init; } = [];

    [JsonPropertyName("diagnostics")]
    public required BeatGridCandidateDiagnostics Diagnostics { get; init; }

    [JsonPropertyName("phase_alignment")]
    public BeatGridPhaseAlignmentDiagnostics? PhaseAlignment { get; init; }

    [JsonPropertyName("agreement_confidence")]
    public BeatGridAgreementConfidenceDiagnostics? AgreementConfidence { get; init; }

    [JsonPropertyName("weak_windows")]
    public BeatGridWeakWindowDiagnostics? WeakWindows { get; init; }

    [JsonPropertyName("weak_window_corrections")]
    public BeatGridWeakWindowCorrectionDiagnostics? WeakWindowCorrections { get; init; }

    [JsonPropertyName("weak_window_correction_plan")]
    public BeatGridWeakWindowCorrectionPlan? WeakWindowCorrectionPlan { get; init; }

    [JsonPropertyName("hybrid_selection")]
    public BeatGridHybridSelectionDiagnostics? HybridSelection { get; init; }

    public static BeatGridCandidateSet None { get; } = new()
    {
        Diagnostics = new BeatGridCandidateDiagnostics
        {
            Enabled = false,
            SelectionReason = "not-enabled"
        }
    };

    public BeatGridCandidateSet WithAgreementConfidence(
        BeatGridAgreementConfidenceDiagnostics agreementConfidence)
    {
        ArgumentNullException.ThrowIfNull(agreementConfidence);

        return Copy(
            selected: Selected,
            diagnostics: Diagnostics,
            agreementConfidence: agreementConfidence,
            weakWindows: WeakWindows,
            weakWindowCorrections: WeakWindowCorrections,
            weakWindowCorrectionPlan: WeakWindowCorrectionPlan,
            correctedExperimental: CorrectedExperimental,
            all: All,
            hybridSelection: HybridSelection);
    }

    public BeatGridCandidateSet WithWeakWindows(BeatGridWeakWindowDiagnostics weakWindows)
    {
        ArgumentNullException.ThrowIfNull(weakWindows);

        return Copy(
            selected: Selected,
            diagnostics: Diagnostics,
            agreementConfidence: AgreementConfidence,
            weakWindows: weakWindows,
            weakWindowCorrections: WeakWindowCorrections,
            weakWindowCorrectionPlan: WeakWindowCorrectionPlan,
            correctedExperimental: CorrectedExperimental,
            all: All,
            hybridSelection: HybridSelection);
    }

    public BeatGridCandidateSet WithWeakWindowCorrection(BeatGridWeakWindowCorrectionResult correction)
    {
        ArgumentNullException.ThrowIfNull(correction);

        var all = All.ToList();
        if (correction.CorrectedCandidate is not null
            && !all.Any(candidate => string.Equals(candidate.Id, correction.CorrectedCandidate.Id, StringComparison.OrdinalIgnoreCase)))
        {
            all.Add(correction.CorrectedCandidate);
        }

        return Copy(
            selected: Selected,
            diagnostics: Diagnostics,
            agreementConfidence: AgreementConfidence,
            weakWindows: WeakWindows,
            weakWindowCorrections: correction.Diagnostics,
            weakWindowCorrectionPlan: correction.Plan,
            correctedExperimental: correction.CorrectedCandidate,
            all: all,
            hybridSelection: HybridSelection);
    }

    public BeatGridCandidateSet WithHybridSelection(
        BeatGridCandidate selected,
        BeatGridHybridSelectionDiagnostics hybridSelection)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(hybridSelection);

        var diagnostics = new BeatGridCandidateDiagnostics
        {
            Enabled = Diagnostics.Enabled,
            CandidateCount = Diagnostics.CandidateCount,
            SelectedCandidateId = selected.Id,
            SelectedSource = selected.Source,
            SelectionReason = hybridSelection.FinalOutputSource == "corrected-experimental"
                ? "explicit-hybrid-selected-corrected-experimental"
                : "explicit-hybrid-fallback-to-primary",
            LegacyCandidateId = Diagnostics.LegacyCandidateId,
            AdvisorCandidateId = Diagnostics.AdvisorCandidateId,
            AdvisorAvailable = Diagnostics.AdvisorAvailable,
            AdvisorAcceptedAsCandidate = Diagnostics.AdvisorAcceptedAsCandidate,
            AdvisorRejectionReason = Diagnostics.AdvisorRejectionReason,
            Notes = Diagnostics.Notes.Concat(["Explicit hybrid selection applied."]).ToArray()
        };

        return Copy(
            selected,
            diagnostics,
            AgreementConfidence,
            WeakWindows,
            WeakWindowCorrections,
            WeakWindowCorrectionPlan,
            CorrectedExperimental,
            All,
            hybridSelection);
    }

    private BeatGridCandidateSet Copy(
        BeatGridCandidate? selected,
        BeatGridCandidateDiagnostics diagnostics,
        BeatGridAgreementConfidenceDiagnostics? agreementConfidence,
        BeatGridWeakWindowDiagnostics? weakWindows,
        BeatGridWeakWindowCorrectionDiagnostics? weakWindowCorrections,
        BeatGridWeakWindowCorrectionPlan? weakWindowCorrectionPlan,
        BeatGridCandidate? correctedExperimental,
        IReadOnlyList<BeatGridCandidate> all,
        BeatGridHybridSelectionDiagnostics? hybridSelection)
    {
        return new BeatGridCandidateSet
        {
            Legacy = Legacy,
            Advisor = Advisor,
            CorrectedExperimental = correctedExperimental,
            Selected = selected,
            All = all,
            Diagnostics = diagnostics,
            PhaseAlignment = PhaseAlignment,
            AgreementConfidence = agreementConfidence,
            WeakWindows = weakWindows,
            WeakWindowCorrections = weakWindowCorrections,
            WeakWindowCorrectionPlan = weakWindowCorrectionPlan,
            HybridSelection = hybridSelection
        };
    }
}
