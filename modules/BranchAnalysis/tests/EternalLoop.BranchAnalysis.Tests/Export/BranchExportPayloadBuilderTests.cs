using System.Text.Json;
using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Preprocessing;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Export;

public sealed class BranchExportPayloadBuilderTests
{
    [Fact]
    public void BuildShouldCreateRootPayload()
    {
        (TrackAnalysisDocument track, BranchGraphData data) = CreateExportFixture();

        BranchExportPayload payload = BranchExportPayloadBuilder.Build(track, data);

        payload.SchemaVersion.Should().Be(BranchExportPayloadBuilder.BranchExportSchemaVersion);
        payload.SourcePage.Should().Be(BranchExportPayloadBuilder.SourcePage);
        payload.BranchSource.Should().Be("track.analysis.beats[*].neighbors");
        payload.ExportedAt.Should().NotBeNullOrWhiteSpace();
        payload.Track.Should().NotBeNull();
        payload.Tuning.Should().NotBeNull();
        payload.Policy.Should().NotBeNull();
        payload.Counts.Should().NotBeNull();
        payload.Diagnostics.Should().NotBeNull();
        payload.ActiveBranches.Should().NotBeEmpty();
        payload.CandidateBranches.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildShouldIncludeTrackInfo()
    {
        (TrackAnalysisDocument track, BranchGraphData data) = CreateExportFixture();

        BranchExportPayload payload = BranchExportPayloadBuilder.Build(track, data);

        payload.Track.Id.Should().Be("export-fixture");
        payload.Track.Title.Should().Be("Export Fixture");
        payload.Track.Artist.Should().Be("Local Artist");
        payload.Track.FixedTitle.Should().Be("Export Fixture by Local Artist");
        payload.Track.Duration.Should().Be(8);
    }

    [Fact]
    public void BuildShouldIncludeTuning()
    {
        (TrackAnalysisDocument track, BranchGraphData data) = CreateExportFixture();

        BranchExportPayload payload = BranchExportPayloadBuilder.Build(track, data);

        payload.Tuning.QuantumType.Should().Be("beats");
        payload.Tuning.CurrentThreshold.Should().Be(35);
        payload.Tuning.ComputedThreshold.Should().Be(35);
        payload.Tuning.MaxBranches.Should().Be(4);
        payload.Tuning.MaxBranchThreshold.Should().Be(80);
        payload.Tuning.AddLastEdge.Should().BeTrue();
        payload.Tuning.JustBackwards.Should().BeFalse();
        payload.Tuning.JustLongBranches.Should().BeFalse();
        payload.Tuning.RemoveSequentialBranches.Should().BeTrue();
        payload.Tuning.MinLongBranch.Should().Be(2);
        payload.Tuning.LastBranchPoint.Should().Be(5);
        payload.Tuning.LongestReach.Should().Be(75);
        payload.Tuning.StructuralPolicy.Should().BeTrue();
        payload.Tuning.AntiLocalLoopPolicy.Should().BeTrue();
        payload.Tuning.ShortBranchPolicy.Should().Be("structural-gated");
        payload.Tuning.ScoreGate.Should().Be(BranchExportPayloadBuilder.ThresholdGate);
        payload.Tuning.StructuralMode.Should().Be(BranchExportPayloadBuilder.StructuralModeEnabled);
    }

    [Fact]
    public void BuildShouldIncludeCoherentCounts()
    {
        (TrackAnalysisDocument track, BranchGraphData data) = CreateExportFixture();

        BranchExportPayload payload = BranchExportPayloadBuilder.Build(track, data);

        payload.Counts.Sections.Should().Be(track.Analysis.Sections.Count);
        payload.Counts.Bars.Should().Be(track.Analysis.Bars.Count);
        payload.Counts.Beats.Should().Be(track.Analysis.Beats.Count);
        payload.Counts.Tatums.Should().Be(track.Analysis.Tatums.Count);
        payload.Counts.Segments.Should().Be(track.Analysis.Segments.Count);
        payload.Counts.ActiveBranches.Should().Be(payload.ActiveBranches.Count);
        payload.Counts.CandidateBranches.Should().Be(payload.CandidateBranches.Count);
        payload.Counts.VisualBranchCount.Should().Be(data.BranchCount);
        payload.Counts.DeletedBranches.Should().Be(data.DeletedEdgeCount);
        payload.Counts.LocalLoopRiskBranches.Should().Be(data.LocalLoopRiskBranches);
        payload.Counts.StructurallyRejectedBranches.Should().Be(data.StructurallyRejectedBranches);
        payload.Counts.AntiMRemovedBranches.Should().Be(data.AntiMRemovedBranches);
    }

    [Fact]
    public void CollectActiveBranchesForExportShouldCollectNeighborsWithActiveStatus()
    {
        (TrackAnalysisDocument track, _) = CreateExportFixture();

        List<BranchExportBranch> branches = BranchExportPayloadBuilder.CollectActiveBranchesForExport(track);

        branches.Should().HaveCount(1);
        branches[0].Status.Should().Be(BranchExportPayloadBuilder.ActiveStatus);
        branches[0].FromBeat.Should().Be(4);
        branches[0].ToBeat.Should().Be(0);
    }

    [Fact]
    public void CollectCandidateBranchesForExportShouldCollectAllNeighborsWithCandidateStatus()
    {
        (TrackAnalysisDocument track, _) = CreateExportFixture();

        List<BranchExportBranch> branches = BranchExportPayloadBuilder.CollectCandidateBranchesForExport(track);

        branches.Should().HaveCount(2);
        branches.Should().OnlyContain(branch => branch.Status == BranchExportPayloadBuilder.CandidateStatus);
    }

    [Fact]
    public void SanitizeSegmentForExportShouldNotExportPitchesOrTimbre()
    {
        SegmentQuantum segment = Segment(0.1, 0.2, [1, 2, 3], [4, 5]);

        BranchExportSegment exported = BranchExportPayloadBuilder.SanitizeSegmentForExport(segment)!;
        string json = JsonSerializer.Serialize(exported);

        json.Should().NotContain("pitches");
        json.Should().NotContain("timbre");
        exported.Start.Should().Be(0.1);
        exported.LoudnessStart.Should().Be(-10);
    }

    [Fact]
    public void SanitizeQuantumForExportShouldIncludeOverlappingSegments()
    {
        (TrackAnalysisDocument track, _) = CreateExportFixture();
        TimeQuantum beat = track.Analysis.Beats[4];

        BranchExportQuantum quantum = BranchExportPayloadBuilder.SanitizeQuantumForExport(beat)!;

        quantum.OverlappingSegmentCount.Should().Be(beat.OverlappingSegments.Count);
        quantum.OverlappingSegments.Should().HaveCount(beat.OverlappingSegments.Count);
        quantum.OverlappingSegments.Should().OnlyContain(segment => segment.Start.HasValue);
    }

    [Fact]
    public void CompareBranchesForExportShouldSortByFromToAndDistance()
    {
        BranchExportBranch first = new() { FromBeat = 1, ToBeat = 4, Distance = 20 };
        BranchExportBranch second = new() { FromBeat = 1, ToBeat = 5, Distance = 1 };
        BranchExportBranch third = new() { FromBeat = 1, ToBeat = 4, Distance = 30 };
        BranchExportBranch nullFrom = new() { FromBeat = null, ToBeat = 0, Distance = 0 };
        List<BranchExportBranch> branches = [nullFrom, third, second, first];

        branches.Sort(BranchExportPayloadBuilder.CompareBranchesForExport);

        branches.Should().Equal(first, third, second, nullFrom);
    }

    [Fact]
    public void SafeNumberShouldReturnNullForNonFiniteValues()
    {
        BranchExportPayloadBuilder.SafeNumber(double.NaN).Should().BeNull();
        BranchExportPayloadBuilder.SafeNumber(double.PositiveInfinity).Should().BeNull();
        BranchExportPayloadBuilder.SafeNumber(double.NegativeInfinity).Should().BeNull();
        BranchExportPayloadBuilder.SafeNumber(12.5).Should().Be(12.5);
    }

    [Fact]
    public void SafeStringShouldConvertValues()
    {
        BranchExportPayloadBuilder.SafeString(null).Should().BeNull();
        BranchExportPayloadBuilder.SafeString("value").Should().Be("value");
        BranchExportPayloadBuilder.SafeString(42).Should().Be("42");
    }

    [Theory]
    [InlineData("Song", "Artist", "Song by Artist")]
    [InlineData("", "Artist", "Unknown Title")]
    [InlineData("(unknown title)", "Artist", "Unknown Title")]
    [InlineData("undefined", "Artist", "Unknown Title")]
    [InlineData("Song", "(unknown artist)", "Song")]
    [InlineData("Song", null, "Song")]
    public void GetTitleShouldMatchNodeBehavior(string? title, string? artist, string expected)
    {
        BranchExportPayloadBuilder.GetTitle(title, artist).Should().Be(expected);
    }

    [Fact]
    public void SanitizeBranchForExportShouldSetDirection()
    {
        TimeQuantum source = new() { Which = 4 };
        BranchExportPayloadBuilder.SanitizeBranchForExport(Edge(source, new TimeQuantum { Which = 0 }, 1, 10), source, "active")
            .Direction.Should().Be(BranchExportPayloadBuilder.BackwardDirection);
        BranchExportPayloadBuilder.SanitizeBranchForExport(Edge(source, new TimeQuantum { Which = 6 }, 2, 10), source, "active")
            .Direction.Should().Be(BranchExportPayloadBuilder.ForwardDirection);
        BranchExportPayloadBuilder.SanitizeBranchForExport(Edge(source, new TimeQuantum { Which = 4 }, 3, 10), source, "active")
            .Direction.Should().Be(BranchExportPayloadBuilder.SelfDirection);
    }

    [Fact]
    public void SanitizeBranchQualityForExportShouldIncludeGateAndPolicyReasons()
    {
        TimeQuantum source = new() { Which = 4 };
        BranchEdge edge = Edge(source, new TimeQuantum { Which = 0 }, 1, 20);
        edge.PolicyReasons = ["long-jump", "same-bar-phase"];

        BranchExportQuality quality = BranchExportPayloadBuilder.SanitizeBranchQualityForExport(edge)!;

        quality.ThresholdGate.Should().Be(BranchExportPayloadBuilder.ThresholdGate);
        quality.PolicyReasons.Should().Equal("long-jump", "same-bar-phase");
        quality.PolicyDecision.Should().Be("accepted");
    }

    private static (TrackAnalysisDocument Track, BranchGraphData Data) CreateExportFixture()
    {
        TrackAnalysisDocument track = new()
        {
            Info = new TrackInfo
            {
                Id = "export-fixture",
                Title = "Export Fixture",
                Name = "Export Fixture Name",
                Artist = "Local Artist"
            },
            AudioSummary = new AudioSummary { Duration = 8 },
            Analysis = new AnalysisData
            {
                Sections = [Quantum(0, 8)],
                Bars = [Quantum(0, 4), Quantum(4, 4)],
                Beats =
                [
                    Quantum(0, 1),
                    Quantum(1, 1),
                    Quantum(2, 1),
                    Quantum(3, 1),
                    Quantum(4, 1),
                    Quantum(5, 1),
                    Quantum(6, 1),
                    Quantum(7, 1)
                ],
                Tatums =
                [
                    Quantum(0, 1),
                    Quantum(1, 1),
                    Quantum(2, 1),
                    Quantum(3, 1),
                    Quantum(4, 1),
                    Quantum(5, 1),
                    Quantum(6, 1),
                    Quantum(7, 1)
                ],
                Segments =
                [
                    Segment(0.1, 0.2, [1, 0, 0], [1, 0]),
                    Segment(1.1, 0.2, [2, 0, 0], [0, 1]),
                    Segment(2.1, 0.2, [3, 0, 0], [1, 0]),
                    Segment(3.1, 0.2, [4, 0, 0], [0, 1]),
                    Segment(4.1, 0.2, [1.1, 0, 0], [1, 0]),
                    Segment(5.1, 0.2, [2.1, 0, 0], [0, 1]),
                    Segment(6.1, 0.2, [3.1, 0, 0], [1, 0]),
                    Segment(7.1, 0.2, [4.1, 0, 0], [0, 1])
                ]
            }
        };

        TrackPreprocessor.Preprocess(track);

        BranchEdge active = Edge(track.Analysis.Beats[4], track.Analysis.Beats[0], 10, 24);
        BranchEdge candidate = Edge(track.Analysis.Beats[5], track.Analysis.Beats[1], 11, 28);

        track.Analysis.Beats[4].Neighbors = [active];
        track.Analysis.Beats[4].AllNeighbors = [active];
        track.Analysis.Beats[5].AllNeighbors = [candidate];

        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);
        data.CurrentThreshold = 35;
        data.ComputedThreshold = 35;
        data.RemoveSequentialBranches = true;
        data.MinLongBranch = 2;
        data.LastBranchPoint = 5;
        data.LongestReach = 75;
        data.BranchCount = 1;
        data.DeletedEdgeCount = 1;
        data.LocalLoopRiskBranches = 1;
        data.StructurallyRejectedBranches = 2;
        data.AntiMRemovedBranches = 3;
        data.StructuralContext = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        return (track, data);
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

    private static SegmentQuantum Segment(double start, double duration, List<double> timbre, List<double> pitches)
    {
        return new SegmentQuantum
        {
            Start = start,
            Duration = duration,
            Confidence = 1,
            LoudnessStart = -10,
            LoudnessMax = -2,
            LoudnessMaxTime = 0.05,
            Timbre = timbre,
            Pitches = pitches
        };
    }

    private static BranchEdge Edge(TimeQuantum source, TimeQuantum destination, int id, double distance)
    {
        return new BranchEdge
        {
            Id = id,
            Source = source,
            Destination = destination,
            Distance = distance,
            AcousticDistance = distance,
            BranchScore = distance + 2,
            StructuralPenalty = 2,
            StructuralBonus = 0,
            StructuralBonusDiagnosticOnly = 4,
            JumpBeatsAbs = Math.Abs(destination.Which - source.Which),
            JumpBars = Math.Abs(destination.Which - source.Which) / 4.0,
            SameBarPhase = true,
            SamePhrasePhase4 = true,
            SamePhrasePhase8 = true,
            SamePhrasePhase16 = true,
            SectionChange = false,
            ShortLocalRisk = true,
            LocalLoopRisk = true,
            PolicyDecision = "accepted",
            PolicyReasons = ["same-bar-phase", "short-local-risk"]
        };
    }
}
