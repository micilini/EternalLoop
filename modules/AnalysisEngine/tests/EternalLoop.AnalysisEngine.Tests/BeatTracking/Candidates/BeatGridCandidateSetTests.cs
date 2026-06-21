using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Candidates;

public sealed class BeatGridCandidateSetTests
{
    [Fact]
    public void None_is_disabled()
    {
        BeatGridCandidateSet.None.Diagnostics.Enabled.Should().BeFalse();
        BeatGridCandidateSet.None.Diagnostics.SelectionReason.Should().Be("not-enabled");
        BeatGridCandidateSet.None.PhaseAlignment.Should().BeNull();
        BeatGridCandidateSet.None.AgreementConfidence.Should().BeNull();
        BeatGridCandidateSet.None.WeakWindows.Should().BeNull();
    }

    [Fact]
    public void Shadow_set_has_selected_legacy()
    {
        var set = CreateShadowSet(advisor: CreateResult("beat-this"));

        set.Selected.Should().BeSameAs(set.Legacy);
        set.Diagnostics.SelectedSource.Should().Be(BeatGridCandidateSourceKind.LegacyBuiltIn);
    }

    [Fact]
    public void Shadow_set_all_contains_legacy_and_advisor_when_available()
    {
        var set = CreateShadowSet(advisor: CreateResult("beat-this"));

        set.All.Select(candidate => candidate.Id).Should().Equal("legacy", "beat-this-advisor");
    }

    [Fact]
    public void Shadow_set_all_contains_only_legacy_when_advisor_unavailable()
    {
        var set = CreateShadowSet(advisor: null);

        set.All.Select(candidate => candidate.Id).Should().Equal("legacy");
        set.Advisor.Should().BeNull();
    }

    [Fact]
    public void CandidateSet_WithAgreementConfidence_preserves_candidates_and_selection()
    {
        var set = CreateShadowSet(advisor: CreateResult("beat-this"));
        var agreement = Core.BeatTracking.Agreement.BeatGridAgreementConfidenceDiagnostics.NotAvailable("test");

        var updated = set.WithAgreementConfidence(agreement);

        updated.Legacy.Should().BeSameAs(set.Legacy);
        updated.Advisor.Should().BeSameAs(set.Advisor);
        updated.Selected.Should().BeSameAs(set.Selected);
        updated.All.Should().BeSameAs(set.All);
        updated.PhaseAlignment.Should().BeSameAs(set.PhaseAlignment);
        updated.AgreementConfidence.Should().BeSameAs(agreement);
    }

    [Fact]
    public void CandidateSet_WithWeakWindows_preserves_candidates_selection_phase_alignment_and_agreement()
    {
        var set = CreateShadowSet(advisor: CreateResult("beat-this"));
        var agreement = Core.BeatTracking.Agreement.BeatGridAgreementConfidenceDiagnostics.NotAvailable("test");
        var withAgreement = set.WithAgreementConfidence(agreement);
        var weakWindows = Core.BeatTracking.WeakWindows.BeatGridWeakWindowDiagnostics.NotAvailable("test");

        var updated = withAgreement.WithWeakWindows(weakWindows);

        updated.Legacy.Should().BeSameAs(set.Legacy);
        updated.Advisor.Should().BeSameAs(set.Advisor);
        updated.Selected.Should().BeSameAs(set.Selected);
        updated.All.Should().BeSameAs(set.All);
        updated.PhaseAlignment.Should().BeSameAs(set.PhaseAlignment);
        updated.AgreementConfidence.Should().BeSameAs(agreement);
        updated.WeakWindows.Should().BeSameAs(weakWindows);
    }

    [Fact]
    public void WithWeakWindowCorrection_adds_corrected_candidate_to_all_without_selecting_it()
    {
        var set = CreateShadowSet(advisor: CreateResult("beat-this"));
        var corrected = new BeatGridCandidate
        {
            Id = "weak-window-corrected-experimental",
            Source = BeatGridCandidateSourceKind.WeakWindowCorrectedExperimental,
            Role = BeatGridCandidateRole.CorrectedExperimental,
            ProviderName = "hybrid-experimental",
            BeatTimes = [0.0, 0.5],
            Quality = new BeatGridCandidateQuality { BeatCount = 2, IsPlausible = true }
        };
        var correction = new Core.BeatTracking.Correction.BeatGridWeakWindowCorrectionResult
        {
            CorrectedCandidate = corrected,
            Plan = new Core.BeatTracking.Correction.BeatGridWeakWindowCorrectionPlan(),
            Diagnostics = Core.BeatTracking.Correction.BeatGridWeakWindowCorrectionDiagnostics.NotAvailable("test")
        };

        var updated = set.WithWeakWindowCorrection(correction);

        updated.CorrectedExperimental.Should().BeSameAs(corrected);
        updated.All.Should().Contain(corrected);
        updated.Selected.Should().BeSameAs(set.Selected);
    }

    private static BeatGridCandidateSet CreateShadowSet(BeatTrackingResult? advisor)
    {
        return new BeatGridCandidateFactory().CreateShadowSet(
            CreateResult("built-in"),
            advisor,
            advisorAvailable: advisor is not null);
    }

    private static BeatTrackingResult CreateResult(string provider)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            ProviderName = provider
        };
    }
}
