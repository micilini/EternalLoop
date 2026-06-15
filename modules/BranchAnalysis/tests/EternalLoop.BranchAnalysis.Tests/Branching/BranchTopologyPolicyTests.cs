using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Models;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Branching;

public sealed class BranchTopologyPolicyTests
{
    [Fact]
    public void ReduceLocalLoopBranchesShouldNotRemoveWhenPolicyDisabled()
    {
        TrackAnalysisDocument track = CreateManualTrack(32);
        BranchGraphData data = new() { AntiLocalLoopPolicy = false };
        BranchEdge edge = CreateEdge(0, track.Analysis.Beats[10], track.Analysis.Beats[18], 20);
        track.Analysis.Beats[10].Neighbors.Add(edge);
        data.AllEdges.Add(edge);

        BranchTopologyResult result = BranchTopologyPolicy.ReduceLocalLoopBranches(
            track,
            data,
            "beats",
            CreateContext(track),
            StructuralBranchOptions.Normalize(antiLocalLoopPolicy: false));

        result.RemovedBranches.Should().Be(0);
        data.AntiMRemovedBranches.Should().Be(0);
        track.Analysis.Beats[10].Neighbors.Should().Contain(edge);
    }

    [Fact]
    public void CollectShortLocalClustersShouldCollectOnlyClustersWithMultipleEdges()
    {
        TrackAnalysisDocument track = CreateManualTrack(64);
        StructuralBranchContext context = CreateContext(track);
        BranchEdge first = CreateEdge(0, track.Analysis.Beats[10], track.Analysis.Beats[18], 20);
        BranchEdge second = CreateEdge(1, track.Analysis.Beats[11], track.Analysis.Beats[19], 19);
        BranchEdge single = CreateEdge(2, track.Analysis.Beats[40], track.Analysis.Beats[45], 18);
        track.Analysis.Beats[10].Neighbors.Add(first);
        track.Analysis.Beats[11].Neighbors.Add(second);
        track.Analysis.Beats[40].Neighbors.Add(single);

        Dictionary<string, List<BranchEdge>> clusters = BranchTopologyPolicy.CollectShortLocalClusters(track, "beats", context);

        clusters.Should().ContainSingle();
        clusters.Values.Single().Should().BeEquivalentTo([first, second]);
    }

    [Fact]
    public void ChooseClusterRemovalsShouldRemoveEntireClusterWithoutStructuralEvidence()
    {
        TrackAnalysisDocument track = CreateManualTrack(32);
        List<BranchEdge> cluster =
        [
            CreateEdge(0, track.Analysis.Beats[10], track.Analysis.Beats[18], 20),
            CreateEdge(1, track.Analysis.Beats[11], track.Analysis.Beats[19], 19)
        ];

        List<BranchEdge> removals = BranchTopologyPolicy.ChooseClusterRemovals(cluster, 1);

        removals.Should().Equal(cluster);
    }

    [Fact]
    public void ChooseClusterRemovalsShouldKeepBestWhenStructuralEvidenceExists()
    {
        TrackAnalysisDocument track = CreateManualTrack(32);
        BranchEdge best = CreateEdge(0, track.Analysis.Beats[10], track.Analysis.Beats[18], 10, edge =>
        {
            edge.PolicyReasons.Add("structural-boundary");
            edge.BranchScore = 10;
        });
        BranchEdge second = CreateEdge(1, track.Analysis.Beats[11], track.Analysis.Beats[19], 12, edge =>
        {
            edge.PolicyReasons.Add("structural-boundary");
            edge.BranchScore = 12;
        });
        BranchEdge third = CreateEdge(2, track.Analysis.Beats[12], track.Analysis.Beats[20], 14, edge =>
        {
            edge.PolicyReasons.Add("structural-boundary");
            edge.BranchScore = 14;
        });

        List<BranchEdge> removals = BranchTopologyPolicy.ChooseClusterRemovals([second, third, best], 1);

        removals.Should().BeEquivalentTo([second, third]);
        removals.Should().NotContain(best);
    }

    [Fact]
    public void ReduceLocalLoopBranchesShouldMarkRemovedEdges()
    {
        TrackAnalysisDocument track = CreateManualTrack(32);
        BranchGraphData data = new();
        BranchEdge first = CreateEdge(0, track.Analysis.Beats[10], track.Analysis.Beats[18], 20);
        BranchEdge second = CreateEdge(1, track.Analysis.Beats[11], track.Analysis.Beats[19], 19);
        track.Analysis.Beats[10].Neighbors.Add(first);
        track.Analysis.Beats[11].Neighbors.Add(second);
        data.AllEdges.AddRange([first, second]);

        BranchTopologyResult result = BranchTopologyPolicy.ReduceLocalLoopBranches(
            track,
            data,
            "beats",
            CreateContext(track),
            StructuralBranchOptions.Normalize(maxShortLocalBranchesPerCluster: 0));

        result.RemovedBranches.Should().Be(2);
        first.Deleted.Should().BeTrue();
        first.LocalLoopRisk.Should().BeTrue();
        first.PolicyDecision.Should().Be("removed-local-loop");
        first.PolicyReasons.Should().Contain("anti-m-removed");
        track.Analysis.Beats[10].Neighbors.Should().NotContain(first);
        data.DeletedEdgeCount.Should().Be(2);
        data.AntiMRemovedBranches.Should().Be(2);
    }

    [Fact]
    public void CountLocalLoopRiskBranchesShouldCountOnlyActiveBranches()
    {
        TrackAnalysisDocument track = CreateManualTrack(8);
        BranchEdge active = CreateEdge(0, track.Analysis.Beats[2], track.Analysis.Beats[0], 20);
        BranchEdge deleted = CreateEdge(1, track.Analysis.Beats[3], track.Analysis.Beats[0], 20, edge => edge.Deleted = true);
        BranchEdge inactive = CreateEdge(2, track.Analysis.Beats[4], track.Analysis.Beats[0], 20);
        track.Analysis.Beats[2].Neighbors.Add(active);
        track.Analysis.Beats[3].Neighbors.Add(deleted);

        int count = BranchTopologyPolicy.CountLocalLoopRiskBranches(track);

        count.Should().Be(1);
        inactive.LocalLoopRisk.Should().BeTrue();
    }

    private static StructuralBranchContext CreateContext(TrackAnalysisDocument track)
    {
        StructuralBranchContext context = StructuralBranchPolicy.BuildStructuralBranchContext(track);
        return new StructuralBranchContext
        {
            Name = context.Name,
            Enabled = context.Enabled,
            Options = context.Options,
            BeatsPerBar = context.BeatsPerBar,
            TotalBeats = context.TotalBeats,
            VeryShortJumpBeats = context.VeryShortJumpBeats,
            ShortJumpBeats = 16,
            PhraseWindowBeats = 32,
            BeatContexts = context.BeatContexts
        };
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

    private static BranchEdge CreateEdge(
        int id,
        TimeQuantum source,
        TimeQuantum destination,
        double distance,
        Action<BranchEdge>? configure = null)
    {
        BranchEdge edge = new()
        {
            Id = id,
            Source = source,
            Destination = destination,
            Distance = distance,
            AcousticDistance = distance,
            BranchScore = distance,
            JumpBeatsAbs = Math.Abs(destination.Which - source.Which),
            ShortLocalRisk = true,
            LocalLoopRisk = true,
            SectionChange = false,
            PolicyDecision = "accepted",
            PolicyReasons = ["short-local-risk"]
        };

        configure?.Invoke(edge);
        return edge;
    }
}
