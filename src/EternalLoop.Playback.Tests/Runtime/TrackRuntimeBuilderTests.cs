using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Runtime;

public sealed class TrackRuntimeBuilderTests
{
    [Fact]
    public void BuildShouldCreateLinkedRuntimeTrack()
    {
        RuntimeTrackBuildResult result = new TrackRuntimeBuilder().Build(PlaybackFixtures.BuildRequest(
            activeBranches: [PlaybackFixtures.Branch()],
            candidateBranches: [PlaybackFixtures.Branch(id: 20, fromBeat: 0, toBeat: 2, distance: 8)]));

        result.Track.Beats.Should().HaveCount(5);
        result.Track.Beats[0].Next.Should().BeSameAs(result.Track.Beats[1]);
        result.Track.Beats[1].Prev.Should().BeSameAs(result.Track.Beats[0]);
        result.Track.ActiveBranchCount.Should().Be(1);
        result.Track.CandidateBranchCount.Should().Be(1);
        result.Track.Beats[1].Neighbors.Should().HaveCount(1);
        result.Track.Beats[0].AllNeighbors.Should().HaveCount(1);
    }

    [Fact]
    public void BuildShouldRejectEmptyBeats()
    {
        TrackRuntimeBuildRequest request = PlaybackFixtures.BuildRequest() with { Beats = [] };

        Action act = () => new TrackRuntimeBuilder().Build(request);

        act.Should().Throw<RuntimeBuildException>();
    }

    [Fact]
    public void BuildShouldIgnoreInvalidDeletedAndSelfBranches()
    {
        RuntimeTrackBuildResult result = new TrackRuntimeBuilder().Build(PlaybackFixtures.BuildRequest(
            activeBranches:
            [
                PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 99),
                PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 1),
                PlaybackFixtures.Branch(id: 3, fromBeat: 1, toBeat: 3, deleted: true)
            ]));

        result.Track.ActiveBranchCount.Should().Be(0);
        result.IgnoredActiveBranches.Should().Be(3);
    }
}
