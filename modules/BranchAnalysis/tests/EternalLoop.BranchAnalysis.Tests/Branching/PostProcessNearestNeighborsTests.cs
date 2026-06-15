using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Preprocessing;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Branching;

public sealed class PostProcessNearestNeighborsTests
{
    [Fact]
    public void CalculateReachabilityShouldSetReachOnAllBeats()
    {
        (TrackAnalysisDocument track, BranchGraphData data) = CreateCalculatedFixture();

        PostProcessNearestNeighbors.CalculateReachability(track, data);

        track.Analysis.Beats.Should().OnlyContain(beat => beat.Reach > 0);
        data.TotalBeats.Should().Be(track.Analysis.Beats.Count);
    }

    [Fact]
    public void FindBestLastBeatShouldReturnBranchPoint()
    {
        (TrackAnalysisDocument track, BranchGraphData data) = CreateCalculatedFixture();
        PostProcessNearestNeighbors.CalculateReachability(track, data);

        int lastBranchPoint = PostProcessNearestNeighbors.FindBestLastBeat(track, data);

        lastBranchPoint.Should().BeGreaterThanOrEqualTo(0);
        data.LongestReach.Should().BeGreaterThanOrEqualTo(0);
        data.TotalBeats.Should().Be(track.Analysis.Beats.Count);
    }

    [Fact]
    public void InsertBestBackwardBranchShouldInsertBestBranch()
    {
        TrackAnalysisDocument track = CreateManualTrack(10);
        BranchGraphData data = new();
        BranchEdge edge = CreateEdge(0, track.Analysis.Beats[8], track.Analysis.Beats[1], 60);
        track.Analysis.Beats[8].AllNeighbors.Add(edge);
        data.AllEdges.Add(edge);

        BranchEdge? inserted = PostProcessNearestNeighbors.InsertBestBackwardBranch(track, data, "beats", 10, 65);

        inserted.Should().BeSameAs(edge);
        track.Analysis.Beats[8].Neighbors.Should().Equal(edge);
    }

    [Fact]
    public void InsertBestBackwardBranchShouldNotDuplicateActiveBranch()
    {
        TrackAnalysisDocument track = CreateManualTrack(10);
        BranchGraphData data = new();
        BranchEdge edge = CreateEdge(0, track.Analysis.Beats[8], track.Analysis.Beats[1], 60);
        track.Analysis.Beats[8].AllNeighbors.Add(edge);
        track.Analysis.Beats[8].Neighbors.Add(edge);
        data.AllEdges.Add(edge);

        BranchEdge? inserted = PostProcessNearestNeighbors.InsertBestBackwardBranch(track, data, "beats", 10, 65);

        inserted.Should().BeSameAs(edge);
        track.Analysis.Beats[8].Neighbors.Should().ContainSingle();
    }

    [Fact]
    public void FilterOutBadBranchesShouldRemoveDestinationsAfterLastIndex()
    {
        TrackAnalysisDocument track = CreateManualTrack(6);
        BranchEdge kept = CreateEdge(0, track.Analysis.Beats[0], track.Analysis.Beats[1], 20);
        BranchEdge removed = CreateEdge(1, track.Analysis.Beats[0], track.Analysis.Beats[5], 20);
        track.Analysis.Beats[0].Neighbors.AddRange([kept, removed]);

        PostProcessNearestNeighbors.FilterOutBadBranches(track, "beats", 3);

        track.Analysis.Beats[0].Neighbors.Should().Equal(kept);
    }

    [Fact]
    public void HasSequentialBranchShouldDetectSequentialBranch()
    {
        TrackAnalysisDocument track = CreateManualTrack(4);
        BranchGraphData data = new() { LastBranchPoint = 0 };
        BranchEdge previousEdge = CreateEdge(0, track.Analysis.Beats[1], track.Analysis.Beats[0], 20);
        BranchEdge currentEdge = CreateEdge(1, track.Analysis.Beats[2], track.Analysis.Beats[1], 20);
        track.Analysis.Beats[1].Neighbors.Add(previousEdge);

        bool sequential = PostProcessNearestNeighbors.HasSequentialBranch(track.Analysis.Beats[2], currentEdge, data);

        sequential.Should().BeTrue();
    }

    [Fact]
    public void FilterOutSequentialBranchesShouldRemoveSequentialBranches()
    {
        TrackAnalysisDocument track = CreateManualTrack(4);
        BranchGraphData data = new() { LastBranchPoint = 0 };
        BranchEdge previousEdge = CreateEdge(0, track.Analysis.Beats[1], track.Analysis.Beats[0], 20);
        BranchEdge currentEdge = CreateEdge(1, track.Analysis.Beats[2], track.Analysis.Beats[1], 20);
        track.Analysis.Beats[1].Neighbors.Add(previousEdge);
        track.Analysis.Beats[2].Neighbors.Add(currentEdge);

        PostProcessNearestNeighbors.FilterOutSequentialBranches(track, data);

        track.Analysis.Beats[2].Neighbors.Should().BeEmpty();
    }

    [Fact]
    public void RemoveDeletedEdgesShouldDeleteAndClearDeletedEdges()
    {
        TrackAnalysisDocument track = CreateManualTrack(4);
        BranchGraphData data = new();
        BranchEdge edge = CreateEdge(0, track.Analysis.Beats[2], track.Analysis.Beats[0], 20);
        track.Analysis.Beats[2].Neighbors.Add(edge);
        data.DeletedEdges.Add(edge);

        PostProcessNearestNeighbors.RemoveDeletedEdges(data);

        edge.Deleted.Should().BeTrue();
        track.Analysis.Beats[2].Neighbors.Should().BeEmpty();
        data.DeletedEdgeCount.Should().Be(1);
        data.DeletedEdges.Should().BeEmpty();
    }

    [Fact]
    public void CountActiveBranchesShouldCountNeighborsOnly()
    {
        TrackAnalysisDocument track = CreateManualTrack(4);
        BranchEdge active = CreateEdge(0, track.Analysis.Beats[1], track.Analysis.Beats[0], 20);
        BranchEdge candidate = CreateEdge(1, track.Analysis.Beats[2], track.Analysis.Beats[0], 20);
        track.Analysis.Beats[1].Neighbors.Add(active);
        track.Analysis.Beats[2].AllNeighbors.Add(candidate);

        int count = PostProcessNearestNeighbors.CountActiveBranches(track);

        count.Should().Be(1);
    }

    [Fact]
    public void PostProcessShouldRunFullPipelineAfterNearestNeighbors()
    {
        (TrackAnalysisDocument track, BranchGraphData data) = CreateCalculatedFixture();

        PostProcessNearestNeighbors.PostProcess(track, data);

        data.TotalBeats.Should().Be(track.Analysis.Beats.Count);
        data.LastBranchPoint.Should().BeGreaterThanOrEqualTo(0);
        data.LongestReach.Should().BeGreaterThanOrEqualTo(0);
        data.AntiMRemovedBranches.Should().BeGreaterThanOrEqualTo(0);
        data.LocalLoopRiskBranches.Should().BeGreaterThanOrEqualTo(0);
        data.BranchCount.Should().Be(PostProcessNearestNeighbors.CountActiveBranches(track));
    }

    [Fact]
    public void PostProcess_WithLateAnchorRouting_SelectsAnchorAtOrAfterPreferredStartWhenPossible()
    {
        TrackAnalysisDocument track = CreateManualTrack(20);
        BranchGraphData data = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = true,
            LateAnchorPreferredStartPercent = 80,
            LateAnchorFallbackStartPercent = 66,
            MinLongBranch = 4
        };
        AddActiveEdge(track, 18, 2, 20, data);

        PostProcessNearestNeighbors.PostProcess(track, data);

        data.LastBranchPoint.Should().Be(18);
        data.LateAnchorDecision.Should().Be("existing-preferred-anchor");
        data.LateAnchorBranchesToTarget.Should().Be(0);
    }

    [Fact]
    public void PostProcess_WithLateAnchorRouting_InsertsPreferredLateAnchorBeforeFallbackAnchor()
    {
        TrackAnalysisDocument track = CreateManualTrack(20);
        BranchGraphData data = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = true,
            CurrentThreshold = 30,
            LateAnchorPreferredStartPercent = 80,
            LateAnchorFallbackStartPercent = 66,
            MinLongBranch = 4
        };
        BranchEdge preferred = AddCandidateEdge(track, 18, 2, 40, data);
        AddCandidateEdge(track, 14, 1, 35, data);

        PostProcessNearestNeighbors.PostProcess(track, data);

        data.LastBranchPoint.Should().Be(18);
        data.LateAnchorDecision.Should().Be("inserted-preferred-anchor");
        data.LateAnchorInsertedEdgeId.Should().Be(preferred.Id);
        track.Analysis.Beats[18].Neighbors.Should().Contain(preferred);
    }

    [Fact]
    public void PostProcess_WithLateAnchorRouting_FallsBackToLegacyWhenNoAnchorIsEligible()
    {
        TrackAnalysisDocument track = CreateManualTrack(10);
        BranchGraphData data = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = true,
            CurrentThreshold = 10
        };
        BranchEdge legacy = AddCandidateEdge(track, 8, 4, 60, data);

        PostProcessNearestNeighbors.PostProcess(track, data);

        track.Analysis.Beats[8].Neighbors.Should().Contain(legacy);
        data.LateAnchorDecision.Should().Be("legacy-fallback");
    }

    [Fact]
    public void PostProcess_LateAnchorRoutingDisabled_ReproducesLegacyLastBranchPoint()
    {
        TrackAnalysisDocument legacyTrack = CreateManualTrack(10);
        BranchGraphData legacyData = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = false,
            CurrentThreshold = 10
        };
        AddCandidateEdge(legacyTrack, 8, 1, 60, legacyData);
        TrackAnalysisDocument routedTrack = CreateManualTrack(10);
        BranchGraphData routedData = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = true,
            CurrentThreshold = 10
        };
        AddCandidateEdge(routedTrack, 8, 1, 60, routedData);

        PostProcessNearestNeighbors.PostProcess(legacyTrack, legacyData);
        PostProcessNearestNeighbors.PostProcess(routedTrack, routedData);

        legacyData.LastBranchPoint.Should().Be(routedData.LastBranchPoint);
        legacyTrack.Analysis.Beats[8].Neighbors.Count.Should().Be(routedTrack.Analysis.Beats[8].Neighbors.Count);
    }

    [Fact]
    public void PostProcess_FilterOutBadBranches_StillRemovesBranchesPastLastBranchPoint()
    {
        TrackAnalysisDocument track = CreateManualTrack(20);
        BranchGraphData data = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = true
        };
        BranchEdge anchor = AddActiveEdge(track, 18, 2, 20, data);
        BranchEdge removed = AddActiveEdge(track, 0, 19, 20, data);

        PostProcessNearestNeighbors.PostProcess(track, data);

        data.LastBranchPoint.Should().Be(18);
        track.Analysis.Beats[0].Neighbors.Should().NotContain(removed);
        track.Analysis.Beats[18].Neighbors.Should().Contain(anchor);
    }

    [Fact]
    public void PostProcess_SequentialFilter_StillSkipsSelectedLastBranchPoint()
    {
        TrackAnalysisDocument track = CreateManualTrack(20);
        BranchGraphData data = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = true,
            RemoveSequentialBranches = true
        };
        AddActiveEdge(track, 16, 0, 20, data);
        BranchEdge previous = AddActiveEdge(track, 17, 1, 20, data);
        BranchEdge selected = AddActiveEdge(track, 18, 2, 20, data);

        PostProcessNearestNeighbors.PostProcess(track, data);

        track.Analysis.Beats[17].Neighbors.Should().BeEmpty();
        track.Analysis.Beats[18].Neighbors.Should().Contain(selected);
        previous.Deleted.Should().BeFalse();
    }

    [Fact]
    public void PostProcess_AntiLocalLoopPolicy_StillRunsAfterLateAnchorRouting()
    {
        TrackAnalysisDocument track = CreateManualTrack(20);
        BranchGraphData data = new()
        {
            AddLastEdge = true,
            LateAnchorRouting = true,
            AntiLocalLoopPolicy = true,
            StructuralContext = StructuralBranchPolicy.BuildStructuralBranchContext(track)
        };
        AddActiveEdge(track, 18, 2, 20, data);

        PostProcessNearestNeighbors.PostProcess(track, data);

        data.LocalLoopRiskBranches.Should().BeGreaterThanOrEqualTo(0);
        data.BranchCount.Should().Be(PostProcessNearestNeighbors.CountActiveBranches(track));
    }

    [Fact]
    public void LongestBackwardBranchShouldMeasurePercent()
    {
        TrackAnalysisDocument track = CreateManualTrack(10);
        BranchEdge edge = CreateEdge(0, track.Analysis.Beats[8], track.Analysis.Beats[3], 20);
        track.Analysis.Beats[8].Neighbors.Add(edge);

        double percent = PostProcessNearestNeighbors.LongestBackwardBranch(track);

        percent.Should().Be(50);
    }

    private static (TrackAnalysisDocument Track, BranchGraphData Data) CreateCalculatedFixture()
    {
        TrackAnalysisDocument track = CreateTrackFixture();
        TrackPreprocessor.Preprocess(track);
        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);
        NearestNeighborCalculator.DynamicCalculateNearestNeighbors(track, data);
        return (track, data);
    }

    private static TrackAnalysisDocument CreateManualTrack(int count)
    {
        TrackAnalysisDocument track = new()
        {
            Analysis = new AnalysisData
            {
                Beats = Enumerable.Range(0, count)
                    .Select(index => new TimeQuantum
                    {
                        Start = index,
                        Duration = 1,
                        Confidence = 1,
                        Which = index
                    })
                    .ToList()
            }
        };

        for (int index = 0; index < track.Analysis.Beats.Count; index++)
        {
            track.Analysis.Beats[index].Prev = index > 0 ? track.Analysis.Beats[index - 1] : null;
            track.Analysis.Beats[index].Next = index < track.Analysis.Beats.Count - 1
                ? track.Analysis.Beats[index + 1]
                : null;
        }

        return track;
    }

    private static TrackAnalysisDocument CreateTrackFixture()
    {
        return new TrackAnalysisDocument
        {
            Info = new TrackInfo { Id = "post-process-fixture", Title = "Post Process Fixture", Artist = "Local" },
            AudioSummary = new AudioSummary { Duration = 8 },
            Analysis = new AnalysisData
            {
                Sections = [Quantum(0, 8)],
                Bars = [Quantum(0, 4), Quantum(4, 4)],
                Beats = Enumerable.Range(0, 8).Select(index => Quantum(index, 1)).ToList(),
                Tatums = Enumerable.Range(0, 8).Select(index => Quantum(index, 1)).ToList(),
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
    }

    private static TimeQuantum Quantum(double start, double duration)
    {
        return new TimeQuantum { Start = start, Duration = duration, Confidence = 1 };
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

    private static BranchEdge CreateEdge(int id, TimeQuantum source, TimeQuantum destination, double distance)
    {
        return new BranchEdge
        {
            Id = id,
            Source = source,
            Destination = destination,
            Distance = distance,
            AcousticDistance = distance,
            BranchScore = distance,
            JumpBeatsAbs = Math.Abs(destination.Which - source.Which),
            PolicyDecision = "accepted",
            PolicyReasons = []
        };
    }

    private static BranchEdge AddActiveEdge(TrackAnalysisDocument track, int source, int destination, double distance, BranchGraphData data)
    {
        BranchEdge edge = AddCandidateEdge(track, source, destination, distance, data);
        track.Analysis.Beats[source].Neighbors.Add(edge);
        return edge;
    }

    private static BranchEdge AddCandidateEdge(TrackAnalysisDocument track, int source, int destination, double distance, BranchGraphData data)
    {
        BranchEdge edge = CreateEdge(data.AllEdges.Count, track.Analysis.Beats[source], track.Analysis.Beats[destination], distance);
        track.Analysis.Beats[source].AllNeighbors.Add(edge);
        data.AllEdges.Add(edge);
        return edge;
    }
}
