using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Shadow;

public sealed class BeatGridShadowAnalyzerTests
{
    [Fact]
    public void Compare_identical_grids_returns_f1_one()
    {
        var analyzer = new BeatGridShadowAnalyzer();

        var comparison = analyzer.Compare(
            CreateResult([0.0, 0.5, 1.0, 1.5], 120.0),
            CreateResult([0.0, 0.5, 1.0, 1.5], 120.0));

        comparison.F1_50Ms.Should().Be(1.0);
        comparison.F1_70Ms.Should().Be(1.0);
        comparison.F1_100Ms.Should().Be(1.0);
        comparison.BestOffsetF1_70Ms.Should().Be(1.0);
    }

    [Fact]
    public void Compare_shifted_grid_reports_lower_zero_offset_and_best_offset()
    {
        var analyzer = new BeatGridShadowAnalyzer();

        var comparison = analyzer.Compare(
            CreateResult([0.0, 0.5, 1.0, 1.5], 120.0),
            CreateResult([0.09, 0.59, 1.09, 1.59], 120.0));

        comparison.F1_70Ms.Should().Be(0.0);
        comparison.BestOffsetMs.Should().Be(-90.0);
        comparison.BestOffsetF1_70Ms.Should().Be(1.0);
    }

    [Fact]
    public void Compare_empty_advisor_returns_zero_agreement()
    {
        var analyzer = new BeatGridShadowAnalyzer();

        var comparison = analyzer.Compare(
            CreateResult([0.0, 0.5, 1.0, 1.5], 120.0),
            CreateResult([], 120.0));

        comparison.Precision70Ms.Should().Be(0.0);
        comparison.Recall70Ms.Should().Be(0.0);
        comparison.F1_70Ms.Should().Be(0.0);
        comparison.BestOffsetF1_70Ms.Should().Be(0.0);
    }

    [Fact]
    public void Compare_count_ratio_uses_advisor_over_legacy()
    {
        var analyzer = new BeatGridShadowAnalyzer();

        var comparison = analyzer.Compare(
            CreateResult([0.0, 0.5, 1.0, 1.5], 120.0),
            CreateResult([0.0, 0.5], 120.0));

        comparison.CountRatio.Should().Be(0.5);
    }

    [Fact]
    public void Compare_bpm_delta_uses_advisor_minus_legacy()
    {
        var analyzer = new BeatGridShadowAnalyzer();

        var comparison = analyzer.Compare(
            CreateResult([0.0, 0.5, 1.0, 1.5], 120.0),
            CreateResult([0.0, 0.5, 1.0, 1.5], 123.5));

        comparison.BpmDelta.Should().Be(3.5);
    }

    [Fact]
    public void Compare_best_offset_uses_same_sign_as_phase_alignment()
    {
        var shadowAnalyzer = new BeatGridShadowAnalyzer();
        var phaseAnalyzer = new BeatGridPhaseAlignmentAnalyzer();
        var legacy = CreateResult([0.0, 0.5, 1.0, 1.5], 120.0);
        var advisor = CreateResult([0.04, 0.54, 1.04, 1.54], 120.0);

        var comparison = shadowAnalyzer.Compare(legacy, advisor);
        var alignment = phaseAnalyzer.Analyze(
            CreateCandidate("legacy", legacy.BeatTimes),
            CreateCandidate("beat-this-advisor", advisor.BeatTimes));

        comparison.BestOffsetMs.Should().Be(-40.0);
        alignment.BestOffsetMs.Should().Be(comparison.BestOffsetMs);
    }

    private static BeatTrackingResult CreateResult(double[] beatTimes, double bpm)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = bpm,
            BeatTimes = beatTimes,
            Confidences = Enumerable.Repeat(0.9, beatTimes.Length).ToArray()
        };
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
            Quality = new BeatGridCandidateQuality
            {
                BeatCount = beatTimes.Length,
                IsPlausible = true
            }
        };
    }
}
