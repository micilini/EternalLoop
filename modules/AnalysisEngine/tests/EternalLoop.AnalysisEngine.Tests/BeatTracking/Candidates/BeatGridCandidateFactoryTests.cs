using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Candidates;

public sealed class BeatGridCandidateFactoryTests
{
    [Fact]
    public void FromResult_creates_legacy_candidate_with_quality()
    {
        var factory = new BeatGridCandidateFactory();

        var candidate = factory.FromResult(
            CreateResult(),
            BeatGridCandidateSourceKind.LegacyBuiltIn,
            BeatGridCandidateRole.SafeAuthority,
            "legacy");

        candidate.Id.Should().Be("legacy");
        candidate.Source.Should().Be(BeatGridCandidateSourceKind.LegacyBuiltIn);
        candidate.Role.Should().Be(BeatGridCandidateRole.SafeAuthority);
        candidate.Quality.BeatCount.Should().Be(4);
        candidate.Quality.DownbeatCount.Should().Be(1);
        candidate.Quality.MedianIntervalSeconds.Should().Be(0.5);
        candidate.Quality.IsPlausible.Should().BeTrue();
    }

    [Fact]
    public void FromResult_marks_dense_grid_when_bpm_above_200()
    {
        var factory = new BeatGridCandidateFactory();

        var candidate = factory.FromResult(CreateResult(bpm: 240.0), BeatGridCandidateSourceKind.BeatThisAdvisor, BeatGridCandidateRole.Advisor, "advisor");

        candidate.Quality.IsDenseGrid.Should().BeTrue();
        candidate.Quality.IsPlausible.Should().BeFalse();
        candidate.Quality.RejectionReason.Should().StartWith("bpm-too-high");
    }

    [Fact]
    public void FromResult_marks_dense_grid_when_density_above_limit()
    {
        var factory = new BeatGridCandidateFactory();
        var result = CreateResult(Enumerable.Range(0, 20).Select(index => index * 0.1).ToArray(), bpm: 120.0);

        var candidate = factory.FromResult(result, BeatGridCandidateSourceKind.BeatThisAdvisor, BeatGridCandidateRole.Advisor, "advisor");

        candidate.Quality.IsDenseGrid.Should().BeTrue();
        candidate.Quality.RejectionReason.Should().StartWith("beat-density-too-high");
    }

    [Fact]
    public void FromResult_marks_implausible_when_no_beats()
    {
        var factory = new BeatGridCandidateFactory();

        var candidate = factory.FromResult(CreateResult([], bpm: 120.0), BeatGridCandidateSourceKind.LegacyBuiltIn, BeatGridCandidateRole.SafeAuthority, "legacy");

        candidate.Quality.BeatCount.Should().Be(0);
        candidate.Quality.IsPlausible.Should().BeFalse();
        candidate.Quality.RejectionReason.Should().Be("beat-count-zero");
    }

    [Fact]
    public void CreateShadowSet_selects_legacy_as_final()
    {
        var factory = new BeatGridCandidateFactory();

        var set = factory.CreateShadowSet(CreateResult(), CreateResult(provider: "beat-this"), advisorAvailable: true);

        set.Selected.Should().BeSameAs(set.Legacy);
        set.Diagnostics.SelectedCandidateId.Should().Be("legacy");
        set.Diagnostics.SelectionReason.Should().Be("shadow-mode-selects-primary");
    }

    [Fact]
    public void CreateShadowSet_includes_advisor_when_available()
    {
        var factory = new BeatGridCandidateFactory();

        var set = factory.CreateShadowSet(CreateResult(), CreateResult(provider: "beat-this"), advisorAvailable: true);

        set.Advisor.Should().NotBeNull();
        set.All.Should().HaveCount(2);
        set.Diagnostics.AdvisorAvailable.Should().BeTrue();
        set.Diagnostics.AdvisorAcceptedAsCandidate.Should().BeTrue();
    }

    [Fact]
    public void CreateShadowSet_records_advisor_rejection_reason()
    {
        var factory = new BeatGridCandidateFactory();

        var set = factory.CreateShadowSet(
            CreateResult(),
            CreateResult(provider: "beat-this"),
            "beat-this-guardrail-rejected:beat-count-too-low",
            advisorAvailable: true);

        set.Advisor.Should().NotBeNull();
        set.Advisor!.Quality.RejectionReason.Should().Be("beat-this-guardrail-rejected:beat-count-too-low");
        set.Diagnostics.AdvisorAcceptedAsCandidate.Should().BeFalse();
        set.Diagnostics.AdvisorRejectionReason.Should().Be("beat-this-guardrail-rejected:beat-count-too-low");
    }

    [Fact]
    public void CreateShadowSet_with_failed_advisor_keeps_legacy_only()
    {
        var factory = new BeatGridCandidateFactory();

        var set = factory.CreateShadowSet(
            CreateResult(),
            advisor: null,
            advisorRejectionReason: "beat-this-provider-failed:model exploded",
            advisorAvailable: false);

        set.Legacy.Should().NotBeNull();
        set.Advisor.Should().BeNull();
        set.All.Should().ContainSingle();
        set.Diagnostics.AdvisorAvailable.Should().BeFalse();
        set.Diagnostics.AdvisorRejectionReason.Should().Be("beat-this-provider-failed:model exploded");
    }

    [Fact]
    public void CreateShadowSet_preserves_phase_alignment_diagnostics()
    {
        var factory = new BeatGridCandidateFactory();
        var phaseAlignment = BeatGridPhaseAlignmentDiagnostics.NotAvailable("advisor-not-available");

        var set = factory.CreateShadowSet(
            CreateResult(),
            advisor: null,
            advisorAvailable: false,
            phaseAlignment: phaseAlignment);

        set.PhaseAlignment.Should().BeSameAs(phaseAlignment);
    }

    [Fact]
    public void CreateShadowSet_does_not_create_agreement_confidence()
    {
        var factory = new BeatGridCandidateFactory();

        var set = factory.CreateShadowSet(CreateResult(), CreateResult(provider: "beat-this"), advisorAvailable: true);

        set.AgreementConfidence.Should().BeNull();
    }

    private static BeatTrackingResult CreateResult(
        double[]? beatTimes = null,
        double bpm = 120.0,
        string provider = "built-in")
    {
        var beats = beatTimes ?? [0.0, 0.5, 1.0, 1.5];

        return new BeatTrackingResult
        {
            EstimatedBpm = bpm,
            BeatTimes = beats,
            Confidences = Enumerable.Repeat(0.9, beats.Length).ToArray(),
            DownbeatTimes = beats.Length > 0 ? [beats[0]] : [],
            ProviderName = provider,
            BeatGridMode = provider
        };
    }
}
