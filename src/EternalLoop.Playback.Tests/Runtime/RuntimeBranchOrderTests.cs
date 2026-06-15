using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Runtime;

public sealed class RuntimeBranchOrderTests
{
    [Fact]
    public void FromLinearBeatIndexShouldCopyNeighborOrder()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        RuntimeBranchOrder order = RuntimeBranchOrder.FromLinearBeatIndex(RuntimeLinearBeatIndex.FromTrack(track));

        IReadOnlyList<RuntimeBranchEdge> candidates = order.GetCandidates(track.Beats[1]);

        candidates.Should().Equal(track.Beats[1].Neighbors, (candidate, neighbor) => ReferenceEquals(candidate, neighbor));
    }

    [Fact]
    public void MoveToEndShouldMoveSelectedBranchOnlyInPrivateOrder()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4),
            PlaybackFixtures.Branch(id: 3, fromBeat: 1, toBeat: 2)
        ]);
        RuntimeBeat seedBeat = track.Beats[1];
        RuntimeBranchEdge selected = seedBeat.Neighbors[0];
        RuntimeBranchOrder order = RuntimeBranchOrder.FromLinearBeatIndex(RuntimeLinearBeatIndex.FromTrack(track));

        order.MoveToEnd(seedBeat, selected);

        IReadOnlyList<RuntimeBranchEdge> candidates = order.GetCandidates(seedBeat);
        candidates[0].Should().BeSameAs(seedBeat.Neighbors[1]);
        candidates[1].Should().BeSameAs(seedBeat.Neighbors[2]);
        candidates[2].Should().BeSameAs(selected);
    }

    [Fact]
    public void MoveToEndShouldNotMutateRuntimeBeatNeighbors()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        RuntimeBeat seedBeat = track.Beats[1];
        RuntimeBranchEdge[] originalNeighbors = seedBeat.Neighbors.ToArray();
        RuntimeBranchOrder order = RuntimeBranchOrder.FromLinearBeatIndex(RuntimeLinearBeatIndex.FromTrack(track));

        order.MoveToEnd(seedBeat, seedBeat.Neighbors[0]);

        seedBeat.Neighbors.Should().Equal(originalNeighbors, (neighbor, original) => ReferenceEquals(neighbor, original));
    }

    [Fact]
    public void MoveToEndShouldUseReferenceIdentity()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 4)
        ]);
        RuntimeBeat seedBeat = track.Beats[1];
        RuntimeBranchEdge[] originalCandidates = seedBeat.Neighbors.ToArray();
        RuntimeBranchOrder order = RuntimeBranchOrder.FromLinearBeatIndex(RuntimeLinearBeatIndex.FromTrack(track));
        RuntimeBranchEdge externalBranch = new()
        {
            Id = originalCandidates[0].Id,
            Status = originalCandidates[0].Status,
            FromBeat = originalCandidates[0].FromBeat,
            ToBeat = originalCandidates[0].ToBeat,
            JumpBeats = originalCandidates[0].JumpBeats,
            Direction = originalCandidates[0].Direction,
            Distance = originalCandidates[0].Distance,
            Deleted = originalCandidates[0].Deleted,
            SourceBeat = originalCandidates[0].SourceBeat,
            DestinationBeat = originalCandidates[0].DestinationBeat
        };

        order.MoveToEnd(seedBeat, externalBranch);

        order.GetCandidates(seedBeat).Should().Equal(originalCandidates, (candidate, original) => ReferenceEquals(candidate, original));
    }

    [Fact]
    public void GetCandidatesShouldReturnEmptyForBeatWithoutBranches()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack();
        RuntimeBranchOrder order = RuntimeBranchOrder.FromLinearBeatIndex(RuntimeLinearBeatIndex.FromTrack(track));

        IReadOnlyList<RuntimeBranchEdge> candidates = order.GetCandidates(track.Beats[1]);

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesShouldNotExposeMutableNeighborList()
    {
        RuntimeTrack track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        RuntimeBeat seedBeat = track.Beats[1];
        RuntimeBranchOrder order = RuntimeBranchOrder.FromLinearBeatIndex(RuntimeLinearBeatIndex.FromTrack(track));

        IReadOnlyList<RuntimeBranchEdge> candidates = order.GetCandidates(seedBeat);

        ReferenceEquals(candidates, seedBeat.Neighbors).Should().BeFalse();
    }
}
