using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridFallbackClassificationTests
{
    [Fact]
    public void SelectExplicitHybrid_classifies_corrected_missing_as_safe_noop()
    {
        var set = CreateSetWithCorrectionDiagnostics(
            rejectionReason: "corrected-experimental-candidate-missing",
            topBlockers: []);

        var (_, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        diagnostics.FallbackCategory.Should().Be(BeatGridHybridFallbackCategory.CorrectedCandidateMissing);
        diagnostics.FallbackIsSafeNoop.Should().BeTrue();
        diagnostics.FallbackIsRuntimeFailure.Should().BeFalse();
    }

    [Fact]
    public void SelectExplicitHybrid_classifies_no_correction_windows_as_safe_noop()
    {
        var set = CreateSetWithNoCorrectionWindows();

        var (_, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        diagnostics.FallbackCategory.Should().Be(BeatGridHybridFallbackCategory.NoCorrectionWindowsAccepted);
        diagnostics.FallbackIsSafeNoop.Should().BeTrue();
    }

    [Fact]
    public void SelectExplicitHybrid_classifies_advisor_missing_as_runtime_failure()
    {
        var legacy = BeatGridHybridSafetyGateTests.CreateCandidate(
            "legacy",
            [0.0, 0.5, 1.0, 1.5],
            BeatGridCandidateSourceKind.LegacyBuiltIn,
            BeatGridCandidateRole.SafeAuthority);
        var set = new BeatGridCandidateSet
        {
            Legacy = legacy,
            Selected = legacy,
            All = [legacy],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true }
        };

        var (_, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        diagnostics.FallbackCategory.Should().Be(BeatGridHybridFallbackCategory.AdvisorUnavailable);
        diagnostics.FallbackIsRuntimeFailure.Should().BeTrue();
        diagnostics.FallbackIsSafeNoop.Should().BeFalse();
    }

    [Fact]
    public void SelectExplicitHybrid_classifies_dense_corrected_as_safe_safety_fallback()
    {
        var corrected = BeatGridHybridSafetyGateTests.CreateCandidate("corrected", [0.0, 0.1, 0.2]);
        var set = CreateSetWithAdvisor(BeatGridHybridSafetyGateTests.CreateSet(corrected: corrected));

        var (_, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        diagnostics.FallbackCategory.Should().Be(BeatGridHybridFallbackCategory.CorrectedCandidateDense);
        diagnostics.FallbackIsSafeNoop.Should().BeTrue();
        diagnostics.FallbackIsRuntimeFailure.Should().BeFalse();
    }

    [Fact]
    public void SelectExplicitHybrid_sets_weak_window_counts()
    {
        var set = CreateSetWithNoCorrectionWindows();

        var (_, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        diagnostics.WeakWindowCount.Should().Be(2);
        diagnostics.FutureCorrectionCandidateCount.Should().Be(1);
        diagnostics.CorrectionCandidateWindowCount.Should().Be(0);
        diagnostics.AcceptedCorrectionWindowCount.Should().Be(0);
        diagnostics.RejectedCorrectionWindowCount.Should().Be(2);
    }

    [Fact]
    public void SelectExplicitHybrid_sets_top_blockers()
    {
        var set = CreateSetWithNoCorrectionWindows();

        var (_, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        diagnostics.TopCorrectionBlockers.Should().Contain("advisor-not-promising");
    }

    private static BeatGridCandidateSet CreateSetWithNoCorrectionWindows()
    {
        var set = BeatGridHybridSafetyGateTests.CreateSet(includeCorrected: false);
        var advisor = BeatGridHybridSafetyGateTests.CreateCandidate(
            "beat-this-advisor",
            [0.0, 0.5, 1.0, 1.5],
            BeatGridCandidateSourceKind.BeatThisAdvisor,
            BeatGridCandidateRole.Advisor);

        return new BeatGridCandidateSet
        {
            Legacy = set.Legacy,
            Advisor = advisor,
            Selected = set.Selected,
            CorrectedExperimental = null,
            All = [set.Legacy!, advisor],
            Diagnostics = set.Diagnostics,
            WeakWindowCorrections = new BeatGridWeakWindowCorrectionDiagnostics
            {
                Enabled = true,
                Status = "rejected",
                Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
                RejectionReason = "no-correction-windows-accepted",
                WeakWindowCount = 2,
                FutureCorrectionCandidateCount = 1,
                CandidateWindowCount = 0,
                AcceptedWindowCount = 0,
                RejectedWindowCount = 2,
                TopBlockers = ["advisor-not-promising"],
                BlockerCounts = new Dictionary<string, int> { ["advisor-not-promising"] = 2 }
            },
            WeakWindowCorrectionPlan = new BeatGridWeakWindowCorrectionPlan
            {
                Enabled = true,
                Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
                WindowCount = 2,
                CandidateWindowCount = 0,
                AcceptedWindowCount = 0,
                RejectedWindowCount = 2,
                TopBlockers = ["advisor-not-promising"],
                BlockerCounts = new Dictionary<string, int> { ["advisor-not-promising"] = 2 }
            },
            WeakWindows = new EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows.BeatGridWeakWindowDiagnostics
            {
                Enabled = true,
                Status = "evaluated",
                WeakWindowCount = 2,
                FutureCorrectionCandidateCount = 1
            }
        };
    }

    private static BeatGridCandidateSet CreateSetWithCorrectionDiagnostics(
        string rejectionReason,
        IReadOnlyList<string> topBlockers)
    {
        var set = CreateSetWithAdvisor(BeatGridHybridSafetyGateTests.CreateSet(includeCorrected: false));
        return new BeatGridCandidateSet
        {
            Legacy = set.Legacy,
            Advisor = set.Advisor,
            Selected = set.Selected,
            CorrectedExperimental = null,
            All = set.All,
            Diagnostics = set.Diagnostics,
            WeakWindowCorrections = new BeatGridWeakWindowCorrectionDiagnostics
            {
                Enabled = true,
                Status = "rejected",
                Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
                RejectionReason = rejectionReason,
                WeakWindowCount = 1,
                FutureCorrectionCandidateCount = 0,
                CandidateWindowCount = 0,
                AcceptedWindowCount = 0,
                RejectedWindowCount = 1,
                TopBlockers = topBlockers
            },
            WeakWindowCorrectionPlan = new BeatGridWeakWindowCorrectionPlan
            {
                Enabled = true,
                Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
                WindowCount = 1,
                CandidateWindowCount = 0,
                AcceptedWindowCount = 0,
                RejectedWindowCount = 1,
                TopBlockers = topBlockers
            }
        };
    }

    private static BeatGridCandidateSet CreateSetWithAdvisor(BeatGridCandidateSet set)
    {
        var advisor = BeatGridHybridSafetyGateTests.CreateCandidate(
            "beat-this-advisor",
            [0.0, 0.5, 1.0, 1.5],
            BeatGridCandidateSourceKind.BeatThisAdvisor,
            BeatGridCandidateRole.Advisor);

        return new BeatGridCandidateSet
        {
            Legacy = set.Legacy,
            Advisor = advisor,
            Selected = set.Selected,
            CorrectedExperimental = set.CorrectedExperimental,
            All = set.CorrectedExperimental is null ? [set.Legacy!, advisor] : [set.Legacy!, advisor, set.CorrectedExperimental],
            Diagnostics = set.Diagnostics,
            WeakWindows = set.WeakWindows,
            WeakWindowCorrections = set.WeakWindowCorrections,
            WeakWindowCorrectionPlan = set.WeakWindowCorrectionPlan
        };
    }
}
