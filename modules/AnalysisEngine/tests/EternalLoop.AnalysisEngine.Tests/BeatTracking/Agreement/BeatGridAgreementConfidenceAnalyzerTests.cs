using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceAnalyzerTests
{
    [Fact]
    public void Analyze_without_advisor_returns_not_available()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSetWithoutAdvisor());

        diagnostics.Status.Should().Be("not-available");
        diagnostics.UnreliableReason.Should().Be("advisor-not-available");
        diagnostics.ShouldModifyFinalGrid.Should().BeFalse();
    }

    [Fact]
    public void Analyze_with_high_phase_alignment_returns_high_or_very_high_confidence()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet(
            phaseAlignment: CreatePhaseAlignment(f1: 0.96, offsetMs: -40.0, countRatio: 1.0, stable: true)));

        diagnostics.GlobalConfidence!.Level.Should().Be(BeatGridAgreementConfidenceLevel.VeryHigh);
        diagnostics.GlobalConfidence.Score.Should().BeGreaterThan(0.90);
        diagnostics.FutureFusionReadiness.Should().Be("candidate-ready");
    }

    [Fact]
    public void Analyze_with_medium_phase_alignment_returns_medium_confidence()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet(
            phaseAlignment: CreatePhaseAlignment(f1: 0.70, offsetMs: 80.0, countRatio: 1.20, stable: true)));

        diagnostics.GlobalConfidence!.Level.Should().Be(BeatGridAgreementConfidenceLevel.Medium);
        diagnostics.FutureFusionReadiness.Should().Be("diagnostic-ready");
    }

    [Fact]
    public void Analyze_with_low_f1_returns_low_or_none_confidence()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet(
            phaseAlignment: CreatePhaseAlignment(f1: 0.35, offsetMs: 0.0, countRatio: 1.0, stable: true, highWindows: 0, lowWindows: 4)));

        diagnostics.GlobalConfidence!.Level.Should().Be(BeatGridAgreementConfidenceLevel.None);
        diagnostics.Status.Should().Be("unreliable");
        diagnostics.FutureFusionReadiness.Should().Be("not-ready");
    }

    [Fact]
    public void Analyze_with_bad_count_ratio_is_not_ready()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet(
            phaseAlignment: CreatePhaseAlignment(f1: 0.96, offsetMs: 0.0, countRatio: 1.8, stable: true)));

        diagnostics.GlobalConfidence!.Level.Should().Be(BeatGridAgreementConfidenceLevel.None);
        diagnostics.FutureFusionReady.Should().BeFalse();
        diagnostics.FutureFusionReadiness.Should().Be("not-ready");
    }

    [Fact]
    public void Analyze_with_dense_advisor_returns_not_available_or_none()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet(
            advisor: CreateCandidate("beat-this-advisor", dense: true),
            phaseAlignment: CreatePhaseAlignment(f1: 0.96, offsetMs: 0.0, countRatio: 1.0, stable: true)));

        diagnostics.Status.Should().Be("not-available");
        diagnostics.UnreliableReason.Should().Be("dense-grid");
    }

    [Fact]
    public void Analyze_high_window_ratio_marks_candidate_ready()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet(
            phaseAlignment: CreatePhaseAlignment(f1: 0.96, offsetMs: 0.0, countRatio: 1.0, stable: true, highWindows: 6, lowWindows: 2)));

        diagnostics.HighConfidenceWindowRatio.Should().Be(0.75);
        diagnostics.FutureFusionReadiness.Should().Be("candidate-ready");
        diagnostics.FutureFusionReady.Should().BeTrue();
    }

    [Fact]
    public void Analyze_low_window_ratio_marks_not_ready()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet(
            phaseAlignment: CreatePhaseAlignment(f1: 0.45, offsetMs: 0.0, countRatio: 1.0, stable: true, highWindows: 0, lowWindows: 8)));

        diagnostics.HighConfidenceWindowRatio.Should().Be(0.0);
        diagnostics.FutureFusionReadiness.Should().Be("not-ready");
        diagnostics.FutureFusionReady.Should().BeFalse();
    }

    [Fact]
    public void Analyze_never_modifies_final_grid()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet());

        diagnostics.ShouldModifyFinalGrid.Should().BeFalse();
        diagnostics.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Analyze_never_selects_advisor()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet());

        diagnostics.ShouldSelectAdvisor.Should().BeFalse();
    }

    [Fact]
    public void Analyze_never_claims_madmom_forever()
    {
        var analyzer = new BeatGridAgreementConfidenceAnalyzer();

        var diagnostics = analyzer.Analyze(CreateCandidateSet());

        diagnostics.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }

    private static BeatGridCandidateSet CreateCandidateSet(
        BeatGridCandidate? advisor = null,
        BeatGridPhaseAlignmentDiagnostics? phaseAlignment = null)
    {
        var legacy = CreateCandidate("legacy");
        var actualAdvisor = advisor ?? CreateCandidate("beat-this-advisor");

        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            Advisor = actualAdvisor,
            Selected = legacy,
            All = actualAdvisor is null ? [legacy] : [legacy, actualAdvisor],
            Diagnostics = new BeatGridCandidateDiagnostics
            {
                Enabled = true,
                SelectedCandidateId = "legacy",
                SelectedSource = BeatGridCandidateSourceKind.LegacyBuiltIn
            },
            PhaseAlignment = phaseAlignment ?? CreatePhaseAlignment(f1: 0.96, offsetMs: 0.0, countRatio: 1.0, stable: true)
        };
    }

    private static BeatGridCandidateSet CreateCandidateSetWithoutAdvisor()
    {
        var legacy = CreateCandidate("legacy");

        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            Advisor = null,
            Selected = legacy,
            All = [legacy],
            Diagnostics = new BeatGridCandidateDiagnostics
            {
                Enabled = true,
                SelectedCandidateId = "legacy",
                SelectedSource = BeatGridCandidateSourceKind.LegacyBuiltIn
            },
            PhaseAlignment = BeatGridPhaseAlignmentDiagnostics.NotAvailable("advisor-not-available")
        };
    }

    private static BeatGridCandidate CreateCandidate(string id, bool dense = false)
    {
        return new BeatGridCandidate
        {
            Id = id,
            Source = id == "legacy" ? BeatGridCandidateSourceKind.LegacyBuiltIn : BeatGridCandidateSourceKind.BeatThisAdvisor,
            Role = id == "legacy" ? BeatGridCandidateRole.SafeAuthority : BeatGridCandidateRole.Advisor,
            ProviderName = id == "legacy" ? "built-in" : "beat-this",
            BeatTimes = Enumerable.Range(0, 64).Select(index => index * 0.5).ToArray(),
            EstimatedBpm = 120.0,
            Quality = new BeatGridCandidateQuality
            {
                BeatCount = 64,
                EstimatedBpm = dense ? 240.0 : 120.0,
                IsDenseGrid = dense,
                IsPlausible = !dense,
                RejectionReason = dense ? "dense-grid" : null
            }
        };
    }

    private static BeatGridPhaseAlignmentDiagnostics CreatePhaseAlignment(
        double f1,
        double offsetMs,
        double countRatio,
        bool stable,
        int highWindows = 4,
        int lowWindows = 0)
    {
        var windows = Enumerable.Range(0, highWindows)
            .Select(index => CreateWindow(index, f1: 0.96, offsetMs: offsetMs, reliable: true))
            .Concat(Enumerable.Range(0, lowWindows).Select(index => CreateWindow(highWindows + index, f1: 0.45, offsetMs: offsetMs, reliable: true)))
            .ToArray();

        return new BeatGridPhaseAlignmentDiagnostics
        {
            Enabled = true,
            Status = f1 >= 0.40 ? "aligned" : "unreliable",
            ReferenceCandidateId = "legacy",
            CandidateId = "beat-this-advisor",
            CountRatio = countRatio,
            BestOffsetMs = offsetMs,
            BestOffset = new BeatGridPhaseAlignmentMetrics { F1_70Ms = f1 },
            OffsetStabilityMadMs = stable ? 0.0 : 60.0,
            IsOffsetStable = stable,
            Confidence = f1 >= 0.85 ? BeatGridPhaseAlignmentConfidence.High : BeatGridPhaseAlignmentConfidence.Low,
            Windows = windows
        };
    }

    private static BeatGridPhaseAlignmentWindow CreateWindow(int index, double f1, double offsetMs, bool reliable)
    {
        return new BeatGridPhaseAlignmentWindow
        {
            Index = index,
            StartTimeSeconds = index * 8.0,
            EndTimeSeconds = (index * 8.0) + 16.0,
            LegacyBeatCount = 32,
            AdvisorBeatCount = 32,
            BestOffsetMs = offsetMs,
            ZeroOffsetF1_70Ms = f1,
            BestOffsetF1_70Ms = f1,
            IsReliable = reliable
        };
    }
}
