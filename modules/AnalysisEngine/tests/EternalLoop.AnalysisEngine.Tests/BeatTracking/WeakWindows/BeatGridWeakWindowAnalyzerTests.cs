using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindowAnalyzerTests
{
    [Fact]
    public void Analyze_without_advisor_returns_not_available()
    {
        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSetWithoutAdvisor());

        diagnostics.Status.Should().Be("not-available");
        diagnostics.UnreliableReason.Should().Be("advisor-not-available");
    }

    [Fact]
    public void Analyze_with_stable_legacy_returns_no_weak_windows()
    {
        var beats = GenerateRegularBeats(80);

        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet(legacyBeats: beats, advisorBeats: beats));

        diagnostics.WeakWindowCount.Should().Be(0);
        diagnostics.FutureCorrectionCandidateCount.Should().Be(0);
        diagnostics.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Analyze_with_irregular_legacy_and_stable_advisor_marks_weak_window()
    {
        var legacy = GenerateRegularBeats(80);
        legacy[20] += 0.35;
        legacy[21] += 0.20;
        legacy[22] -= 0.20;
        legacy[23] += 0.30;
        var advisor = GenerateRegularBeats(80);

        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet(legacyBeats: legacy, advisorBeats: advisor));

        diagnostics.WeakWindowCount.Should().BeGreaterThan(0);
        diagnostics.FutureCorrectionCandidateCount.Should().BeGreaterThan(0);
        diagnostics.Windows.Should().Contain(window => window.Reasons.Contains(BeatGridWeakWindowReason.LegacyTempoInstability));
        diagnostics.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Analyze_with_dense_advisor_blocks_window()
    {
        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet(advisorDense: true));

        diagnostics.Status.Should().Be("not-available");
        diagnostics.UnreliableReason.Should().Be("advisor-rejected-or-dense");
    }

    [Fact]
    public void Analyze_with_bad_count_ratio_marks_high_risk_or_blocked()
    {
        var legacy = GenerateRegularBeats(80);
        var advisor = GenerateRegularBeats(160).Select(beat => beat * 0.5).ToArray();

        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet(legacyBeats: legacy, advisorBeats: advisor, bestF1: 0.95));

        diagnostics.FutureCorrectionCandidateCount.Should().Be(0);
        diagnostics.Windows
            .Any(window => window.RiskLevel is BeatGridWeakWindowRiskLevel.High or BeatGridWeakWindowRiskLevel.Blocked)
            .Should().BeTrue();
    }

    [Fact]
    public void Analyze_with_high_advisor_strength_marks_future_correction_candidate()
    {
        var legacy = GenerateRegularBeats(80);
        legacy[18] += 0.40;
        legacy[19] -= 0.20;
        legacy[20] += 0.30;

        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet(legacyBeats: legacy, advisorBeats: GenerateRegularBeats(80), bestF1: 0.95));

        diagnostics.Windows.Should().Contain(window => window.FutureCorrectionCandidate);
        diagnostics.FutureCorrectionReadiness.Should().Be("candidate-ready");
    }

    [Fact]
    public void Analyze_with_low_advisor_strength_marks_diagnostic_only()
    {
        var legacy = GenerateRegularBeats(80);
        legacy[18] += 0.40;

        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet(legacyBeats: legacy, advisorBeats: GenerateRegularBeats(80), bestF1: 0.45));

        diagnostics.FutureCorrectionCandidateCount.Should().Be(0);
        diagnostics.Windows
            .Any(window => window.CorrectionReadiness is BeatGridWeakWindowCorrectionReadiness.DiagnosticOnly or BeatGridWeakWindowCorrectionReadiness.CandidateForReview)
            .Should().BeTrue();
    }

    [Fact]
    public void Analyze_with_candidate_disagreement_does_not_assume_advisor_is_correct()
    {
        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet(bestF1: 0.30));

        diagnostics.FutureCorrectionCandidateCount.Should().Be(0);
        diagnostics.Windows.Should().Contain(window => window.Reasons.Contains(BeatGridWeakWindowReason.CandidateDisagreement));
    }

    [Fact]
    public void Analyze_never_modifies_final_grid()
    {
        new BeatGridWeakWindowAnalyzer().Analyze(CreateSet()).ShouldModifyFinalGrid.Should().BeFalse();
    }

    [Fact]
    public void Analyze_never_selects_advisor()
    {
        new BeatGridWeakWindowAnalyzer().Analyze(CreateSet()).ShouldSelectAdvisor.Should().BeFalse();
    }

    [Fact]
    public void Analyze_never_applies_correction()
    {
        var diagnostics = new BeatGridWeakWindowAnalyzer().Analyze(CreateSet());

        diagnostics.ShouldApplyCorrection.Should().BeFalse();
        diagnostics.Windows.Should().OnlyContain(window => !window.ShouldApplyCorrection);
    }

    [Fact]
    public void Analyze_never_claims_madmom_forever()
    {
        new BeatGridWeakWindowAnalyzer().Analyze(CreateSet()).ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }

    private static BeatGridCandidateSet CreateSet(
        double[]? legacyBeats = null,
        double[]? advisorBeats = null,
        BeatGridCandidate? advisor = null,
        bool advisorDense = false,
        double bestF1 = 0.95)
    {
        var legacy = CreateCandidate("legacy", legacyBeats ?? GenerateRegularBeats(80));
        var actualAdvisor = advisor ?? CreateCandidate("beat-this-advisor", advisorBeats ?? GenerateRegularBeats(80), advisorDense);

        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            Advisor = actualAdvisor,
            Selected = legacy,
            All = actualAdvisor is null ? [legacy] : [legacy, actualAdvisor],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true, SelectedCandidateId = "legacy" },
            PhaseAlignment = CreatePhaseAlignment(bestF1),
            AgreementConfidence = CreateAgreement(bestF1)
        };
    }

    private static BeatGridCandidateSet CreateSetWithoutAdvisor()
    {
        var legacy = CreateCandidate("legacy", GenerateRegularBeats(80));

        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            Advisor = null,
            Selected = legacy,
            All = [legacy],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true, SelectedCandidateId = "legacy" },
            PhaseAlignment = BeatGridPhaseAlignmentDiagnostics.NotAvailable("advisor-not-available"),
            AgreementConfidence = BeatGridAgreementConfidenceDiagnostics.NotAvailable("advisor-not-available")
        };
    }

    private static BeatGridCandidate CreateCandidate(string id, double[] beats, bool dense = false)
    {
        return new BeatGridCandidate
        {
            Id = id,
            Source = id == "legacy" ? BeatGridCandidateSourceKind.LegacyBuiltIn : BeatGridCandidateSourceKind.BeatThisAdvisor,
            Role = id == "legacy" ? BeatGridCandidateRole.SafeAuthority : BeatGridCandidateRole.Advisor,
            ProviderName = id == "legacy" ? "built-in" : "beat-this",
            BeatTimes = beats,
            EstimatedBpm = 120.0,
            Quality = new BeatGridCandidateQuality
            {
                BeatCount = beats.Length,
                IsDenseGrid = dense,
                IsPlausible = !dense,
                RejectionReason = dense ? "advisor-rejected-or-dense" : null
            }
        };
    }

    private static double[] GenerateRegularBeats(int count)
    {
        return Enumerable.Range(0, count).Select(index => index * 0.5).ToArray();
    }

    private static BeatGridPhaseAlignmentDiagnostics CreatePhaseAlignment(double f1)
    {
        return new BeatGridPhaseAlignmentDiagnostics
        {
            Enabled = true,
            Status = "aligned",
            CountRatio = 1.0,
            BestOffsetMs = 0.0,
            BestOffset = new BeatGridPhaseAlignmentMetrics { F1_70Ms = f1 },
            IsOffsetStable = true,
            Confidence = BeatGridPhaseAlignmentConfidence.High,
            Windows = Enumerable.Range(0, 4).Select(index => new BeatGridPhaseAlignmentWindow
            {
                Index = index,
                StartTimeSeconds = index * 8.0,
                EndTimeSeconds = (index * 8.0) + 15.5,
                LegacyBeatCount = 32,
                AdvisorBeatCount = 32,
                BestOffsetMs = 0.0,
                ZeroOffsetF1_70Ms = f1,
                BestOffsetF1_70Ms = f1,
                IsReliable = true
            }).ToArray()
        };
    }

    private static BeatGridAgreementConfidenceDiagnostics CreateAgreement(double f1)
    {
        return new BeatGridAgreementConfidenceDiagnostics
        {
            Enabled = true,
            Status = "evaluated",
            GlobalConfidence = new BeatGridAgreementConfidenceScore
            {
                Level = BeatGridAgreementConfidenceLevel.High,
                Score = f1,
                F1_70Ms = f1,
                IsReliable = true
            },
            Windows = Enumerable.Range(0, 4).Select(index => new BeatGridAgreementConfidenceWindow
            {
                Index = index,
                StartTimeSeconds = index * 8.0,
                EndTimeSeconds = (index * 8.0) + 15.5,
                BestOffsetMs = 0.0,
                BestOffsetF1_70Ms = f1,
                Confidence = new BeatGridAgreementConfidenceScore
                {
                    Level = BeatGridAgreementConfidenceLevel.High,
                    Score = f1,
                    F1_70Ms = f1,
                    IsReliable = true
                },
                FutureFusionCandidate = true
            }).ToArray()
        };
    }
}
