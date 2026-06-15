using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Preprocessing;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Branching;

public sealed class NearestNeighborCalculatorTests
{
    [Fact]
    public void CreateBranchGraphDataShouldApplyDefaults()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();

        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        data.MaxBranches.Should().Be(4);
        data.SimilarityThreshold.Should().Be(0.86);
        data.LookaheadDepth.Should().Be(1);
        data.MinJumpDistance.Should().Be(4);
        data.MaxBranchThreshold.Should().Be(80);
        data.CurrentThreshold.Should().Be(80);
        data.ComputedThreshold.Should().Be(80);
        data.AddLastEdge.Should().BeTrue();
        data.TotalBeats.Should().Be(track.Analysis.Beats.Count);
    }

    [Fact]
    public void CreateBranchGraphDataShouldApplyOverrides()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();

        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            "beats",
            new NearestNeighborOptions
            {
                MaxBranches = 2,
                SimilarityThreshold = 0.92,
                LookaheadDepth = 2,
                MinJumpDistance = 6,
                MaxBranchThreshold = 25,
                JustBackwards = true,
                JustLongBranches = true,
                MinLongBranch = 4
            });

        data.MaxBranches.Should().Be(2);
        data.SimilarityThreshold.Should().Be(0.92);
        data.LookaheadDepth.Should().Be(2);
        data.MinJumpDistance.Should().Be(6);
        data.MaxBranchThreshold.Should().Be(25);
        data.JustBackwards.Should().BeTrue();
        data.JustLongBranches.Should().BeTrue();
        data.MinLongBranch.Should().Be(4);
    }

    [Fact]
    public void PrecalculateNearestNeighborsShouldCreateCandidateEdges()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);

        track.Analysis.Beats.Should().Contain(beat => beat.AllNeighbors.Count > 0);
        data.AllEdges.Should().NotBeEmpty();
        data.AllEdges[0].PolicyDecision.Should().Be("accepted");
        data.AllEdges[0].PolicyReasons.Should().NotContain("phase7-neutral-policy");
        data.AllEdges[0].PolicyReasons.Should().NotBeEmpty();
        data.StructuralContext.Should().NotBeNull();
    }

    [Fact]
    public void NeutralTuningShouldMatchDefaultBranchCandidates()
    {
        TrackAnalysisDocument firstTrack = CreatePreprocessedTrackFixture();
        BranchGraphData defaultData = NearestNeighborCalculator.CreateBranchGraphData(firstTrack);

        NearestNeighborCalculator.PrecalculateNearestNeighbors(firstTrack, defaultData, "beats", 4, 80);

        TrackAnalysisDocument secondTrack = CreatePreprocessedTrackFixture();
        BranchGraphData neutralData = NearestNeighborCalculator.CreateBranchGraphData(
            secondTrack,
            "beats",
            new NearestNeighborOptions
            {
                SimilarityThreshold = 0.86,
                LookaheadDepth = 1,
                MinJumpDistance = 4,
                MaxBranches = 4,
                MaxBranchThreshold = 80
            });

        NearestNeighborCalculator.PrecalculateNearestNeighbors(secondTrack, neutralData, "beats", 4, 80);

        neutralData.AllEdges.Count.Should().Be(defaultData.AllEdges.Count);
    }

    [Fact]
    public void MinJumpDistanceAboveNeutralShouldFilterShortCandidateBranches()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            "beats",
            new NearestNeighborOptions
            {
                MinJumpDistance = 6,
                MaxBranches = 4,
                MaxBranchThreshold = 80
            });

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);

        data.AllEdges.Should().OnlyContain(edge =>
            edge.Source == null ||
            edge.Destination == null ||
            Math.Abs(edge.Source.Which - edge.Destination.Which) >= 6);
    }

    [Fact]
    public void LookaheadDepthOneShouldNotFilterDefaultCandidates()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            "beats",
            new NearestNeighborOptions
            {
                LookaheadDepth = 1,
                MaxBranches = 4,
                MaxBranchThreshold = 80
            });

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);

        data.AllEdges.Should().NotBeEmpty();
    }

    [Fact]
    public void PrecalculateNearestNeighborsShouldLimitEdgesByMaxBranches()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 1, 80);

        track.Analysis.Beats.Should().OnlyContain(beat => beat.AllNeighbors.Count <= 1);
    }

    [Fact]
    public void CalculateNearestNeighborsForQuantumShouldSkipSelfQuantum()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);
        TimeQuantum source = track.Analysis.Beats[0];

        NearestNeighborCalculator.CalculateNearestNeighborsForQuantum(track, data, "beats", 4, 80, source);

        source.AllNeighbors.Should().OnlyContain(edge => edge.Destination!.Which != source.Which);
    }

    [Fact]
    public void CalculateQuantumDistanceShouldPenalizeSameSegment()
    {
        TrackAnalysisDocument track = CreateSharedSegmentTrackFixture();

        double distance = NearestNeighborCalculator.CalculateQuantumDistance(
            track.Analysis.Beats[0],
            track.Analysis.Beats[1]);

        distance.Should().BeGreaterThanOrEqualTo(100);
    }

    [Fact]
    public void CalculateQuantumDistanceShouldApplyParentPositionPenalty()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        TimeQuantum firstBeat = track.Analysis.Beats[0];

        double sequentialDistance = NearestNeighborCalculator.CalculateQuantumDistance(firstBeat, track.Analysis.Beats[1]);
        double matchingPositionDistance = NearestNeighborCalculator.CalculateQuantumDistance(firstBeat, track.Analysis.Beats[4]);

        sequentialDistance.Should().BeGreaterThanOrEqualTo(matchingPositionDistance + 90);
    }

    [Fact]
    public void CollectNearestNeighborsShouldCollectActiveNeighborsByThreshold()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);
        int branchingCount = NearestNeighborCalculator.CollectNearestNeighbors(track, data, "beats", 80);

        branchingCount.Should().BeGreaterThan(0);
        track.Analysis.Beats.Should().Contain(beat => beat.Neighbors.Count > 0);
    }

    [Fact]
    public void ExtractNearestNeighborsShouldRespectBackwardsOnly()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            "beats",
            new NearestNeighborOptions { JustBackwards = true });

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);
        TimeQuantum lastBeat = track.Analysis.Beats[7];
        List<BranchEdge> neighbors = NearestNeighborCalculator.ExtractNearestNeighbors(data, lastBeat, 80);

        neighbors.Should().NotBeEmpty();
        neighbors.Should().OnlyContain(edge => edge.Destination!.Which < lastBeat.Which);
    }

    [Fact]
    public void ExtractNearestNeighborsShouldRespectLongBranchFilter()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            "beats",
            new NearestNeighborOptions
            {
                JustLongBranches = true,
                MinLongBranch = 4
            });

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);
        List<BranchEdge> neighbors = NearestNeighborCalculator.ExtractNearestNeighbors(data, track.Analysis.Beats[0], 80);

        neighbors.Should().OnlyContain(edge => Math.Abs(edge.Destination!.Which - edge.Source!.Which) >= 4);
    }

    [Fact]
    public void DynamicCalculateNearestNeighborsShouldSetCurrentAndComputedThreshold()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        int branchingCount = NearestNeighborCalculator.DynamicCalculateNearestNeighbors(track, data);

        branchingCount.Should().BeGreaterThan(0);
        data.CurrentThreshold.Should().BeGreaterThan(0);
        data.CurrentThreshold.Should().Be(data.ComputedThreshold);
    }

    [Fact]
    public void NearestNeighborsShouldBeDeterministic()
    {
        List<EdgeSnapshot> first = RunDeterministicPass();
        List<EdgeSnapshot> second = RunDeterministicPass();

        second.Should().Equal(first);
    }

    [Fact]
    public void CandidateThresholdShouldUseAcousticDistanceOnly()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            "beats",
            new NearestNeighborOptions
            {
                MaxBranchThreshold = 10,
                StructuralPolicy = true
            });

        NearestNeighborCalculator.CalculateNearestNeighborsForQuantum(track, data, "beats", 4, 10, track.Analysis.Beats[0]);

        track.Analysis.Beats[0].AllNeighbors.Should().OnlyContain(edge => edge.AcousticDistance < 10);
        track.Analysis.Beats[0].AllNeighbors.Should().OnlyContain(edge => edge.BranchScore >= edge.AcousticDistance);
    }

    [Fact]
    public void ActiveThresholdShouldUseAcousticDistanceOnly()
    {
        TimeQuantum source = new() { Which = 0 };
        TimeQuantum destination = new() { Which = 32 };
        BranchEdge edge = new()
        {
            Source = source,
            Destination = destination,
            Distance = 50,
            AcousticDistance = 50,
            BranchScore = 5,
            Deleted = false
        };
        source.AllNeighbors = [edge];

        List<BranchEdge> neighbors = NearestNeighborCalculator.ExtractNearestNeighbors(new BranchGraphData(), source, 10);

        neighbors.Should().BeEmpty();
    }

    [Fact]
    public void CompareEdgesByAcousticQualityShouldKeepAcousticDistancePrimary()
    {
        TimeQuantum source = new() { Which = 0 };
        BranchEdge betterAcoustic = new()
        {
            Source = source,
            Destination = new TimeQuantum { Which = 40 },
            AcousticDistance = 10,
            Distance = 10,
            StructuralPenalty = 20,
            BranchScore = 30,
            JumpBeatsAbs = 40
        };
        BranchEdge betterScore = new()
        {
            Source = source,
            Destination = new TimeQuantum { Which = 80 },
            AcousticDistance = 20,
            Distance = 20,
            StructuralPenalty = 0,
            BranchScore = 20,
            JumpBeatsAbs = 80
        };
        List<BranchEdge> edges = [betterScore, betterAcoustic];

        edges.Sort(NearestNeighborCalculator.CompareEdgesByAcousticQuality);

        edges[0].Should().BeSameAs(betterAcoustic);
    }

    [Fact]
    public void DynamicCalculateNearestNeighborsShouldHandleEmptyQuanta()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        track.Analysis.Beats = [];
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        int branchingCount = NearestNeighborCalculator.DynamicCalculateNearestNeighbors(track, data);

        branchingCount.Should().Be(0);
        data.CurrentThreshold.Should().Be(0);
        data.ComputedThreshold.Should().Be(0);
        data.AllEdges.Should().BeEmpty();
    }

    [Fact]
    public void PrecalculateNearestNeighborsShouldBeIdempotent()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);
        int firstCount = data.AllEdges.Count;
        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);

        data.AllEdges.Should().HaveCount(firstCount);
    }

    [Fact]
    public void InvalidQuantumTypeShouldThrow()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();

        Action act = () => NearestNeighborCalculator.CreateBranchGraphData(track, "bars-invalid");

        act.Should().Throw<NearestNeighborException>()
            .WithMessage("Track analysis.bars-invalid must be an array.");
    }

    [Fact]
    public void PrecalculateNearestNeighborsShouldUseLegacyPolicyFieldsWhenDisabled()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(
            track,
            "beats",
            new NearestNeighborOptions { StructuralPolicy = false });

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);

        data.AllEdges.Should().NotBeEmpty();
        data.AllEdges.Should().OnlyContain(edge => edge.PolicyDecision == "legacy");
        data.AllEdges.Should().OnlyContain(edge => edge.PolicyReasons.Contains("structural-policy-disabled"));
    }

    [Fact]
    public void PrecalculateNearestNeighborsShouldIncrementStructurallyRejectedBranches()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        NearestNeighborCalculator.PrecalculateNearestNeighbors(track, data, "beats", 4, 80);

        data.StructurallyRejectedBranches.Should().BeGreaterThan(0);
    }

    private static List<EdgeSnapshot> RunDeterministicPass()
    {
        TrackAnalysisDocument track = CreatePreprocessedTrackFixture();
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);

        NearestNeighborCalculator.DynamicCalculateNearestNeighbors(track, data);

        return data.AllEdges
            .Select(edge => new EdgeSnapshot(
                edge.Id,
                edge.Source!.Which,
                edge.Destination!.Which,
                Math.Round(edge.Distance, 6)))
            .ToList();
    }

    private static TrackAnalysisDocument CreatePreprocessedTrackFixture()
    {
        TrackAnalysisDocument track = new()
        {
            Info = new TrackInfo
            {
                Id = "nearest-neighbor-fixture",
                Title = "Nearest Neighbor Fixture",
                Artist = "Local"
            },
            AudioSummary = new AudioSummary
            {
                Duration = 8
            },
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
                    Segment(1.1, 0.2, [5, 0, 0], [0, 1]),
                    Segment(2.1, 0.2, [9, 0, 0], [0.5, 0.5]),
                    Segment(3.1, 0.2, [13, 0, 0], [0.25, 0.75]),
                    Segment(4.1, 0.2, [1.1, 0, 0], [1, 0]),
                    Segment(5.1, 0.2, [5.1, 0, 0], [0, 1]),
                    Segment(6.1, 0.2, [9.1, 0, 0], [0.5, 0.5]),
                    Segment(7.1, 0.2, [13.1, 0, 0], [0.25, 0.75])
                ]
            }
        };

        TrackPreprocessor.Preprocess(track);

        return track;
    }

    private static TrackAnalysisDocument CreateSharedSegmentTrackFixture()
    {
        TrackAnalysisDocument track = new()
        {
            Info = new TrackInfo
            {
                Id = "shared-segment-track",
                Title = "Shared Segment Track",
                Artist = "Local"
            },
            AudioSummary = new AudioSummary
            {
                Duration = 2
            },
            Analysis = new AnalysisData
            {
                Sections = [Quantum(0, 2)],
                Bars = [Quantum(0, 2)],
                Beats = [Quantum(0, 1), Quantum(1, 1)],
                Tatums = [Quantum(0, 1), Quantum(1, 1)],
                Segments = [Segment(0, 2, [1, 1, 1], [1, 0])]
            }
        };

        TrackPreprocessor.Preprocess(track);

        return track;
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
            LoudnessStart = 0,
            LoudnessMax = 0,
            LoudnessMaxTime = 0,
            Timbre = timbre,
            Pitches = pitches
        };
    }

    private sealed record EdgeSnapshot(int Id, int SourceWhich, int DestinationWhich, double Distance);
}
