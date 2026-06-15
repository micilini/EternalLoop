using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Preprocessing;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Branching;

public sealed class StructuralBranchPolicyTests
{
    [Fact]
    public void BuildStructuralBranchContextShouldComputePolicyWindowSizes()
    {
        TrackAnalysisDocument track = CreateGridTrack(32, [32]);

        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        context.Name.Should().Be(StructuralBranchPolicy.PolicyName);
        context.BeatsPerBar.Should().Be(4);
        context.VeryShortJumpBeats.Should().Be(8);
        context.ShortJumpBeats.Should().Be(16);
        context.PhraseWindowBeats.Should().Be(32);
        context.TotalBeats.Should().Be(32);
    }

    [Fact]
    public void BuildStructuralBranchContextShouldEstimateMostFrequentBeatsPerBar()
    {
        TrackAnalysisDocument track = CreateGridTrack(20, [20]);
        TrackPreprocessor.Preprocess(track);
        track.Analysis.Bars[0].Children = [track.Analysis.Beats[0], track.Analysis.Beats[1], track.Analysis.Beats[2]];
        track.Analysis.Bars[1].Children = [track.Analysis.Beats[3], track.Analysis.Beats[4], track.Analysis.Beats[5], track.Analysis.Beats[6], track.Analysis.Beats[7]];
        track.Analysis.Bars[2].Children = [track.Analysis.Beats[8], track.Analysis.Beats[9], track.Analysis.Beats[10], track.Analysis.Beats[11], track.Analysis.Beats[12]];

        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        context.BeatsPerBar.Should().Be(5);
    }

    [Fact]
    public void PolicyDisabledShouldReturnLegacyScore()
    {
        TrackAnalysisDocument track = CreateGridTrack(64, [32, 32]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(
            track,
            "beats",
            StructuralBranchOptions.Normalize(structuralPolicy: false, antiLocalLoopPolicy: false));

        StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
            track.Analysis.Beats[12],
            track.Analysis.Beats[16],
            50,
            context);

        score.PolicyDecision.Should().Be("legacy");
        score.BranchScore.Should().Be(score.AcousticDistance);
        score.StructuralPenalty.Should().Be(0);
        score.StructuralBonusDiagnosticOnly.Should().Be(0);
        score.PolicyReasons.Should().Contain("structural-policy-disabled");
    }

    [Fact]
    public void VeryShortLocalBranchShouldBeRejected()
    {
        TrackAnalysisDocument track = CreateGridTrack(32, [32]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
            track.Analysis.Beats[12],
            track.Analysis.Beats[16],
            2,
            context);

        StructuralBranchPolicy.IsStructurallyAllowedBranch(
            track.Analysis.Beats[12],
            track.Analysis.Beats[16],
            score,
            context).Should().BeFalse();
        StructuralBranchPolicy.IsShortLocalBranch(score).Should().BeTrue();
        score.PolicyReasons.Should().Contain(reason =>
            reason == "rejected-very-short-local" || reason == "rejected-short-local");
    }

    [Fact]
    public void LongAlignedBranchShouldBeAcceptedAndScoreNoLowerThanAcousticDistance()
    {
        TrackAnalysisDocument track = CreateGridTrack(160, [80, 80]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
            track.Analysis.Beats[32],
            track.Analysis.Beats[128],
            12,
            context);

        StructuralBranchPolicy.IsStructurallyAllowedBranch(
            track.Analysis.Beats[32],
            track.Analysis.Beats[128],
            score,
            context).Should().BeTrue();
        score.PolicyReasons.Should().Contain("long-jump");
        score.PolicyReasons.Should().Contain("same-bar-phase");
        score.BranchScore.Should().BeGreaterThanOrEqualTo(score.AcousticDistance);
    }

    [Fact]
    public void ShortBranchWithStrongStructuralEvidenceShouldBeAccepted()
    {
        TrackAnalysisDocument track = CreateGridTrack(64, [32, 32]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
            track.Analysis.Beats[28],
            track.Analysis.Beats[36],
            2,
            context);

        score.JumpBeatsAbs.Should().Be(8);
        score.SectionChange.Should().BeTrue();
        StructuralBranchPolicy.IsStructurallyAllowedBranch(
            track.Analysis.Beats[28],
            track.Analysis.Beats[36],
            score,
            context).Should().BeTrue();
        score.PolicyReasons.Should().Contain("section-change");
        score.PolicyReasons.Should().Contain("structural-boundary");
    }

    [Fact]
    public void BarPhaseMismatchShouldApplyPenalty()
    {
        TrackAnalysisDocument track = CreateGridTrack(64, [64]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
            track.Analysis.Beats[8],
            track.Analysis.Beats[13],
            20,
            context);

        score.StructuralPenalty.Should().BeGreaterThan(0);
        score.PolicyReasons.Should().Contain("bar-phase-mismatch");
    }

    [Fact]
    public void SameBarPhaseShouldAddDiagnosticBonus()
    {
        TrackAnalysisDocument track = CreateGridTrack(64, [64]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
            track.Analysis.Beats[8],
            track.Analysis.Beats[24],
            20,
            context);

        score.StructuralBonusDiagnosticOnly.Should().BeGreaterThan(0);
        score.PolicyReasons.Should().Contain("same-bar-phase");
    }

    [Fact]
    public void ScoreShouldNeverBeatAcousticDistance()
    {
        TrackAnalysisDocument track = CreateGridTrack(160, [80, 80]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
            track.Analysis.Beats[32],
            track.Analysis.Beats[128],
            50,
            context);

        score.StructuralBonusDiagnosticOnly.Should().BeGreaterThan(0);
        score.BranchScore.Should().BeGreaterThanOrEqualTo(score.AcousticDistance);
        score.BranchScore.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public void AttachScoreToEdgeShouldCopyAllFields()
    {
        BranchEdge edge = new();
        StructuralBranchScore score = new()
        {
            AcousticDistance = 1,
            StructuralPenalty = 2,
            StructuralBonus = 3,
            StructuralBonusDiagnosticOnly = 4,
            BranchScore = 5,
            JumpBeatsAbs = 6,
            JumpBars = 1.5,
            SameBarPhase = true,
            SamePhrasePhase4 = true,
            SamePhrasePhase8 = true,
            SamePhrasePhase16 = true,
            SectionChange = true,
            NearStructuralBoundary = true,
            ShortLocalRisk = true,
            LocalLoopRisk = true,
            PolicyDecision = "rejected",
            PolicyReasons = ["reason"]
        };

        StructuralBranchPolicy.AttachScoreToEdge(edge, score);

        edge.AcousticDistance.Should().Be(1);
        edge.StructuralPenalty.Should().Be(2);
        edge.StructuralBonus.Should().Be(3);
        edge.StructuralBonusDiagnosticOnly.Should().Be(4);
        edge.BranchScore.Should().Be(5);
        edge.JumpBeatsAbs.Should().Be(6);
        edge.JumpBars.Should().Be(1.5);
        edge.SameBarPhase.Should().BeTrue();
        edge.SamePhrasePhase4.Should().BeTrue();
        edge.SamePhrasePhase8.Should().BeTrue();
        edge.SamePhrasePhase16.Should().BeTrue();
        edge.SectionChange.Should().BeTrue();
        edge.NearStructuralBoundary.Should().BeTrue();
        edge.ShortLocalRisk.Should().BeTrue();
        edge.LocalLoopRisk.Should().BeTrue();
        edge.PolicyDecision.Should().Be("rejected");
        edge.PolicyReasons.Should().Equal("reason");
    }

    [Fact]
    public void GetPolicySummaryShouldExposePolicyNameAndThresholds()
    {
        TrackAnalysisDocument track = CreateGridTrack(32, [32]);
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        StructuralPolicySummary summary = StructuralBranchPolicy.GetPolicySummary(context);

        summary.Name.Should().Be(StructuralBranchPolicy.PolicyName);
        summary.Enabled.Should().BeTrue();
        summary.MinVeryShortJumpBeats.Should().Be(8);
        summary.MinShortJumpBeats.Should().Be(16);
        summary.PhraseWindowBeats.Should().Be(32);
    }

    [Fact]
    public void IsShortLocalBranchShouldReflectScoreRisk()
    {
        StructuralBranchPolicy.IsShortLocalBranch(new StructuralBranchScore { ShortLocalRisk = true }).Should().BeTrue();
        StructuralBranchPolicy.IsShortLocalBranch(new StructuralBranchScore { ShortLocalRisk = false }).Should().BeFalse();
        StructuralBranchPolicy.IsShortLocalBranch(null).Should().BeFalse();
    }

    private static TrackAnalysisDocument CreateGridTrack(int beatCount, IReadOnlyList<int> sectionBeatLengths)
    {
        TrackAnalysisDocument track = new()
        {
            Info = new TrackInfo
            {
                Id = "grid-track",
                Title = "Grid Track",
                Artist = "Local"
            },
            AudioSummary = new AudioSummary
            {
                Duration = beatCount
            },
            Analysis = new AnalysisData
            {
                Sections = CreateSections(sectionBeatLengths),
                Bars = CreateBars(beatCount),
                Beats = CreateQuanta(beatCount, 1),
                Tatums = CreateQuanta(beatCount, 1),
                Segments = CreateSegments(beatCount)
            }
        };

        TrackPreprocessor.Preprocess(track);
        return track;
    }

    private static List<TimeQuantum> CreateSections(IReadOnlyList<int> sectionBeatLengths)
    {
        List<TimeQuantum> sections = [];
        int sectionStart = 0;

        foreach (int length in sectionBeatLengths)
        {
            sections.Add(Quantum(sectionStart, length));
            sectionStart += length;
        }

        return sections;
    }

    private static List<TimeQuantum> CreateBars(int beatCount)
    {
        List<TimeQuantum> bars = [];

        for (int index = 0; index < beatCount / 4; index++)
        {
            bars.Add(Quantum(index * 4, 4));
        }

        return bars;
    }

    private static List<TimeQuantum> CreateQuanta(int count, double duration)
    {
        List<TimeQuantum> quanta = [];

        for (int index = 0; index < count; index++)
        {
            quanta.Add(Quantum(index * duration, duration));
        }

        return quanta;
    }

    private static List<SegmentQuantum> CreateSegments(int beatCount)
    {
        List<SegmentQuantum> segments = [];

        for (int index = 0; index < beatCount; index++)
        {
            segments.Add(new SegmentQuantum
            {
                Start = index + 0.1,
                Duration = 0.2,
                Confidence = 1,
                LoudnessStart = 0,
                LoudnessMax = 0,
                LoudnessMaxTime = 0,
                Timbre = [index % 4, 0, 0],
                Pitches = [index % 4, 0]
            });
        }

        return segments;
    }

    private static TimeQuantum Quantum(double start, double duration)
    {
        return new TimeQuantum
        {
            Start = start,
            Duration = duration,
            Confidence = 1
        };
    }
}
