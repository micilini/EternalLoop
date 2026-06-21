using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentAnalyzerTests
{
    [Fact]
    public void Analyze_identical_candidates_returns_zero_offset_high_confidence()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", GenerateBeats(64)),
            CreateCandidate("beat-this-advisor", GenerateBeats(64)));

        diagnostics.Status.Should().Be("aligned");
        diagnostics.BestOffsetMs.Should().Be(0.0);
        diagnostics.ZeroOffset!.F1_70Ms.Should().Be(1.0);
        diagnostics.BestOffset!.F1_70Ms.Should().Be(1.0);
        diagnostics.Confidence.Should().Be(BeatGridPhaseAlignmentConfidence.High);
        diagnostics.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Analyze_candidate_shifted_late_returns_negative_offset()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();
        var reference = GenerateBeats(64);
        var lateCandidate = reference.Select(beat => beat + 0.040).ToArray();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", reference),
            CreateCandidate("beat-this-advisor", lateCandidate));

        diagnostics.BestOffsetMs.Should().BeApproximately(-40.0, 0.0001);
        diagnostics.OffsetDirection.Should().Be("candidate-needs-backward-shift");
    }

    [Fact]
    public void Analyze_candidate_shifted_early_returns_positive_offset()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();
        var reference = GenerateBeats(64);
        var earlyCandidate = reference.Select(beat => beat - 0.040).ToArray();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", reference),
            CreateCandidate("beat-this-advisor", earlyCandidate));

        diagnostics.BestOffsetMs.Should().BeApproximately(40.0, 0.0001);
        diagnostics.OffsetDirection.Should().Be("candidate-needs-forward-shift");
    }

    [Fact]
    public void Analyze_offset_improves_f1_reports_improvement()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();
        var reference = GenerateBeats(64);
        var lateCandidate = reference.Select(beat => beat + 0.090).ToArray();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", reference),
            CreateCandidate("beat-this-advisor", lateCandidate));

        diagnostics.ZeroOffset!.F1_70Ms.Should().Be(0.0);
        diagnostics.BestOffsetMs.Should().BeApproximately(-90.0, 0.0001);
        diagnostics.BestOffset!.F1_70Ms.Should().Be(1.0);
        diagnostics.ImprovementF1_70Ms.Should().Be(1.0);
        diagnostics.Status.Should().Be("offset-detected");
    }

    [Fact]
    public void Analyze_poor_count_ratio_marks_unreliable()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", GenerateBeats(64)),
            CreateCandidate("beat-this-advisor", GenerateBeats(16)));

        diagnostics.Status.Should().Be("unreliable");
        diagnostics.Confidence.Should().Be(BeatGridPhaseAlignmentConfidence.None);
        diagnostics.UnreliableReason.Should().Be("count-ratio-out-of-range");
        diagnostics.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Analyze_empty_candidate_returns_not_available()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", GenerateBeats(16)),
            CreateCandidate("beat-this-advisor", []));

        diagnostics.Status.Should().Be("not-available");
        diagnostics.Confidence.Should().Be(BeatGridPhaseAlignmentConfidence.None);
        diagnostics.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Analyze_local_windows_reports_stable_offset()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();
        var reference = GenerateBeats(96);
        var lateCandidate = reference.Select(beat => beat + 0.040).ToArray();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", reference),
            CreateCandidate("beat-this-advisor", lateCandidate));

        diagnostics.Windows.Should().NotBeEmpty();
        diagnostics.Windows.Should().OnlyContain(window => window.BestOffsetMs == -40.0);
        diagnostics.OffsetStabilityMadMs.Should().Be(0.0);
        diagnostics.IsOffsetStable.Should().BeTrue();
    }

    [Fact]
    public void Analyze_unstable_local_offsets_marks_offset_unstable()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();
        var reference = GenerateBeats(128);
        var candidate = reference
            .Select((beat, index) => index < 64 ? beat + 0.040 : beat - 0.080)
            .ToArray();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", reference),
            CreateCandidate("beat-this-advisor", candidate));

        diagnostics.Windows.Should().NotBeEmpty();
        diagnostics.OffsetStabilityMadMs.Should().BeGreaterThan(20.0);
        diagnostics.IsOffsetStable.Should().BeFalse();
    }

    [Fact]
    public void Analyze_never_recommends_correction()
    {
        var analyzer = new BeatGridPhaseAlignmentAnalyzer();
        var reference = GenerateBeats(64);
        var lateCandidate = reference.Select(beat => beat + 0.040).ToArray();

        var diagnostics = analyzer.Analyze(
            CreateCandidate("legacy", reference),
            CreateCandidate("beat-this-advisor", lateCandidate));

        diagnostics.ShouldApplyCorrection.Should().BeFalse();
        diagnostics.Recommendation.Should().Be("diagnostic-only-do-not-correct");
    }

    private static BeatGridCandidate CreateCandidate(string id, double[] beatTimes)
    {
        return new BeatGridCandidate
        {
            Id = id,
            Source = id == "legacy" ? BeatGridCandidateSourceKind.LegacyBuiltIn : BeatGridCandidateSourceKind.BeatThisAdvisor,
            Role = id == "legacy" ? BeatGridCandidateRole.SafeAuthority : BeatGridCandidateRole.Advisor,
            ProviderName = id == "legacy" ? "built-in" : "beat-this",
            BeatTimes = beatTimes,
            Confidences = Enumerable.Repeat(0.9, beatTimes.Length).ToArray(),
            EstimatedBpm = 120.0,
            Quality = new BeatGridCandidateQuality
            {
                BeatCount = beatTimes.Length,
                IsPlausible = beatTimes.Length > 0
            }
        };
    }

    private static double[] GenerateBeats(int count)
    {
        return Enumerable.Range(0, count).Select(index => index * 0.5).ToArray();
    }
}
