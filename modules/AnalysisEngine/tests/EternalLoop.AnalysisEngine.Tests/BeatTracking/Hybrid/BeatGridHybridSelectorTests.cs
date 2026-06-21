using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridSelectorTests
{
    [Fact]
    public void SelectExplicitHybrid_selects_corrected_when_safe()
    {
        var set = BeatGridHybridSafetyGateTests.CreateSet();

        var (selected, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        selected.Should().BeSameAs(set.CorrectedExperimental);
        diagnostics.Decision.Should().Be(BeatGridHybridSelectionDecision.SelectedCorrectedExperimental);
        diagnostics.Status.Should().Be("selected-corrected-experimental");
    }

    [Fact]
    public void SelectExplicitHybrid_falls_back_to_legacy_when_unsafe()
    {
        var set = BeatGridHybridSafetyGateTests.CreateSet(acceptedWindowCount: 0);

        var (selected, diagnostics) = new BeatGridHybridSelector().SelectExplicitHybrid(set);

        selected.Should().BeSameAs(set.Legacy);
        diagnostics.Decision.Should().Be(BeatGridHybridSelectionDecision.FallbackToLegacy);
        diagnostics.Status.Should().Be("fallback-to-legacy");
    }

    [Fact]
    public void SelectExplicitHybrid_falls_back_to_legacy_when_disabled()
    {
        var selector = new BeatGridHybridSelector(new BeatGridHybridSelectionOptions { AllowCorrectedExperimentalAsFinal = false });
        var set = BeatGridHybridSafetyGateTests.CreateSet();

        var (selected, diagnostics) = selector.SelectExplicitHybrid(set);

        selected.Should().BeSameAs(set.Legacy);
        diagnostics.Decision.Should().Be(BeatGridHybridSelectionDecision.SelectedLegacy);
        diagnostics.Status.Should().Be("disabled");
    }

    [Fact]
    public void SelectExplicitHybrid_marks_explicit_opt_in_true()
    {
        new BeatGridHybridSelector().SelectExplicitHybrid(BeatGridHybridSafetyGateTests.CreateSet())
            .Diagnostics.ExplicitOptIn.Should().BeTrue();
    }

    [Fact]
    public void SelectExplicitHybrid_marks_auto_uses_hybrid_false()
    {
        new BeatGridHybridSelector().SelectExplicitHybrid(BeatGridHybridSafetyGateTests.CreateSet())
            .Diagnostics.AutoUsesHybrid.Should().BeFalse();
    }

    [Fact]
    public void SelectExplicitHybrid_never_claims_madmom_forever()
    {
        new BeatGridHybridSelector().SelectExplicitHybrid(BeatGridHybridSafetyGateTests.CreateSet())
            .Diagnostics.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }
}
