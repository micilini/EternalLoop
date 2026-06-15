using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Models;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Branching;

public sealed class LateAnchorRoutingPolicyTests
{
    [Fact]
    public void ResolveEarlyReturnTargetBeat_UsesFallbackWhenNoBackwardBranches()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);

        int target = LateAnchorRoutingPolicy.ResolveEarlyReturnTargetBeat(quanta, 25);

        target.Should().Be(5);
    }

    [Fact]
    public void ResolveEarlyReturnTargetBeat_UsesEarliestBackwardDestination()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        AddActiveEdge(quanta, 10, 8, 20);

        int target = LateAnchorRoutingPolicy.ResolveEarlyReturnTargetBeat(quanta, 25);

        target.Should().Be(8);
    }

    [Fact]
    public void ResolveEarlyReturnTargetBeat_BoundsLateHintBeforeMidTrack()
    {
        List<TimeQuantum> quanta = CreateQuanta(100);
        AddActiveEdge(quanta, 40, 30, 20);
        AddActiveEdge(quanta, 80, 70, 20);

        int target = LateAnchorRoutingPolicy.ResolveEarlyReturnTargetBeat(quanta, 25);

        target.Should().Be(55);
    }

    [Fact]
    public void CalculateBranchesToEarlyReturnTarget_ReturnsZeroBeforeTarget()
    {
        List<TimeQuantum> quanta = CreateQuanta(8);

        IReadOnlyDictionary<int, int> costs = LateAnchorRoutingPolicy.CalculateBranchesToEarlyReturnTarget(quanta, 2);

        costs[0].Should().Be(0);
        costs[1].Should().Be(0);
        costs[2].Should().Be(0);
    }

    [Fact]
    public void CalculateBranchesToEarlyReturnTarget_UsesLinearAdvanceAndBranches()
    {
        List<TimeQuantum> quanta = CreateQuanta(8);
        AddActiveEdge(quanta, 5, 1, 20);

        IReadOnlyDictionary<int, int> costs = LateAnchorRoutingPolicy.CalculateBranchesToEarlyReturnTarget(quanta, 2);

        costs[5].Should().Be(1);
        costs[4].Should().Be(1);
    }

    [Fact]
    public void CalculateBranchesToEarlyReturnTarget_LeavesUnreachableAsInfinity()
    {
        List<TimeQuantum> quanta = CreateQuanta(8);

        IReadOnlyDictionary<int, int> costs = LateAnchorRoutingPolicy.CalculateBranchesToEarlyReturnTarget(quanta, 2);

        costs[7].Should().Be(LateAnchorRoutingPolicy.Unreachable);
    }

    [Fact]
    public void CalculateEarliestReachableByBeat_PropagatesBackwardBranches()
    {
        List<TimeQuantum> quanta = CreateQuanta(8);
        AddActiveEdge(quanta, 6, 1, 20);

        IReadOnlyDictionary<int, int> earliest = LateAnchorRoutingPolicy.CalculateEarliestReachableByBeat(quanta);

        earliest[6].Should().Be(1);
        earliest[5].Should().Be(1);
    }

    [Fact]
    public void FindBestTieredAnchorSource_PrefersLateDirectReturnWhenQualityAcceptable()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        AddActiveEdge(quanta, 12, 2, 10);
        AddActiveEdge(quanta, 18, 3, 20);
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(quanta, 25);

        LateAnchorSourceCandidate? candidate = LateAnchorRoutingPolicy.FindBestTieredAnchorSource(quanta, context, 10, 4);

        candidate.Should().NotBeNull();
        candidate!.SourceIndex.Should().Be(18);
    }

    [Fact]
    public void FindBestTieredAnchorSource_PrefersBetterQualityWhenLateCandidateNeedsMoreBranchesAndIsNearby()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        AddActiveEdge(quanta, 16, 2, 10);
        AddActiveEdge(quanta, 17, 8, 50);
        AddActiveEdge(quanta, 8, 2, 10);
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(quanta, 25);

        LateAnchorSourceCandidate? candidate = LateAnchorRoutingPolicy.FindBestTieredAnchorSource(quanta, context, 10, 4);

        candidate.Should().NotBeNull();
        candidate!.SourceIndex.Should().Be(16);
    }

    [Fact]
    public void FindBestTieredAnchorSource_FallsBackAcrossTierRules()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        AddActiveEdge(quanta, 18, 14, 10);
        AddActiveEdge(quanta, 14, 2, 10);
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(quanta, 25);

        LateAnchorSourceCandidate? candidate = LateAnchorRoutingPolicy.FindBestTieredAnchorSource(quanta, context, 16, 12);

        candidate.Should().NotBeNull();
        candidate!.SourceIndex.Should().Be(18);
        candidate.Cost.BranchesToEarlyReturnTarget.Should().Be(1);
    }

    [Fact]
    public void SelectExistingAnchorSource_ReturnsNullWhenNoBackwardPathReachesTarget()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        AddActiveEdge(quanta, 18, 10, 10);
        LateAnchorDecisionContext context = new()
        {
            EarlyReturnTargetBeat = 5,
            BranchesToEarlyReturnTarget = LateAnchorRoutingPolicy.CalculateBranchesToEarlyReturnTarget(quanta, 5),
            EarliestReachableByBeat = LateAnchorRoutingPolicy.CalculateEarliestReachableByBeat(quanta)
        };

        LateAnchorSourceCandidate? candidate = LateAnchorRoutingPolicy.SelectExistingAnchorSource(
            quanta,
            context,
            10,
            10,
            4);

        candidate.Should().BeNull();
    }

    [Fact]
    public void InsertBestAnchorBranch_UsesAllNeighborsWhenActiveNeighborsLackLateAnchor()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        BranchEdge edge = AddCandidateEdge(quanta, 18, 2, 40);
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(quanta, 25);
        BranchGraphData data = new() { CurrentThreshold = 30 };

        LateAnchorRoutingResult result = LateAnchorRoutingPolicy.InsertBestAnchorBranch(quanta, data, 30, 65, 10, context);

        result.InsertedEdge.Should().BeSameAs(edge);
        quanta[18].Neighbors.Should().Contain(edge);
    }

    [Fact]
    public void InsertBestAnchorBranch_DoesNotInsertDuplicateActiveEdge()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        BranchEdge edge = AddCandidateEdge(quanta, 18, 2, 40);
        quanta[18].Neighbors.Add(edge);
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(quanta, 25);

        LateAnchorRoutingPolicy.InsertBestAnchorBranch(quanta, new BranchGraphData(), 30, 65, 10, context);

        quanta[18].Neighbors.Should().ContainSingle();
    }

    [Fact]
    public void InsertBestAnchorBranch_RespectsMaxThreshold()
    {
        List<TimeQuantum> quanta = CreateQuanta(20);
        AddCandidateEdge(quanta, 18, 2, 70);
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(quanta, 25);

        LateAnchorRoutingResult result = LateAnchorRoutingPolicy.InsertBestAnchorBranch(quanta, new BranchGraphData(), 30, 65, 10, context);

        result.InsertedEdge.Should().BeNull();
        quanta[18].Neighbors.Should().BeEmpty();
    }

    [Fact]
    public void InsertBestAnchorBranch_PrefersFewerBranchesToTargetOverLongerImmediateJump()
    {
        List<TimeQuantum> quanta = CreateQuanta(30);
        AddCandidateEdge(quanta, 28, 20, 10);
        AddActiveEdge(quanta, 20, 2, 10);
        BranchEdge betterCost = AddCandidateEdge(quanta, 24, 2, 20);
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(quanta, 25);

        LateAnchorRoutingResult result = LateAnchorRoutingPolicy.InsertBestAnchorBranch(quanta, new BranchGraphData(), 5, 65, 15, context);

        result.InsertedEdge.Should().BeSameAs(betterCost);
    }

    private static List<TimeQuantum> CreateQuanta(int count)
    {
        List<TimeQuantum> quanta = Enumerable.Range(0, count)
            .Select(index => new TimeQuantum
            {
                Which = index,
                Start = index,
                Duration = 1,
                Confidence = 1
            })
            .ToList();

        for (int index = 0; index < quanta.Count; index++)
        {
            quanta[index].Prev = index > 0 ? quanta[index - 1] : null;
            quanta[index].Next = index < quanta.Count - 1 ? quanta[index + 1] : null;
        }

        return quanta;
    }

    private static BranchEdge AddActiveEdge(List<TimeQuantum> quanta, int source, int destination, double distance)
    {
        BranchEdge edge = AddCandidateEdge(quanta, source, destination, distance);
        quanta[source].Neighbors.Add(edge);
        return edge;
    }

    private static BranchEdge AddCandidateEdge(List<TimeQuantum> quanta, int source, int destination, double distance)
    {
        BranchEdge edge = new()
        {
            Id = source * 100 + destination,
            Source = quanta[source],
            Destination = quanta[destination],
            Distance = distance,
            AcousticDistance = distance,
            BranchScore = distance,
            JumpBeatsAbs = Math.Abs(source - destination),
            PolicyDecision = "accepted",
            PolicyReasons = []
        };

        quanta[source].AllNeighbors.Add(edge);
        return edge;
    }
}
