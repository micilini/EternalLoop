using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Runtime;

public sealed class BranchEscapeGuardTests
{
    [Fact]
    public void EvaluateShouldAllowBranchWhenGuardIsDisabled()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        var guard = new BranchEscapeGuard(new BranchEscapeOptions { Enabled = false });

        BranchEscapeResult result = guard.Evaluate(
            track.Beats[0],
            track.Beats[1],
            track.Beats[1].Neighbors[0],
            track.Beats[0]);

        result.IsSafe.Should().BeTrue();
        result.Reason.Should().Be("Guard disabled");
    }

    [Fact]
    public void EvaluateShouldRejectInvalidSelfBranch()
    {
        var track = PlaybackFixtures.BuildTrack();
        var edge = new Playback.Models.RuntimeBranchEdge
        {
            Id = 1,
            FromBeat = 1,
            ToBeat = 1,
            JumpBeats = 0,
            Direction = "self",
            Distance = 0,
            SourceBeat = track.Beats[1],
            DestinationBeat = track.Beats[1]
        };
        var guard = new BranchEscapeGuard();

        BranchEscapeResult result = guard.Evaluate(track.Beats[0], track.Beats[1], edge, track.Beats[0]);

        result.IsSafe.Should().BeFalse();
        result.Reason.Should().Be("Invalid branch");
    }

    [Fact]
    public void BranchEscapeGuardShouldEvaluateWithPrecomputedLinearIndex()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);
        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromTrack(track);
        var guard = new BranchEscapeGuard(new BranchEscapeOptions { Enabled = false });

        BranchEscapeResult result = guard.Evaluate(
            track.Beats[0],
            track.Beats[1],
            track.Beats[1].Neighbors[0],
            index);

        result.IsSafe.Should().BeTrue();
        result.Reason.Should().Be("Guard disabled");
    }

    [Fact]
    public void BranchEscapeGuardIsInEndZoneShouldUsePrecomputedLinearIndex()
    {
        var track = PlaybackFixtures.BuildTrack();
        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromTrack(track);
        var guard = new BranchEscapeGuard(new BranchEscapeOptions
        {
            Enabled = true,
            EndGuardStartRatio = 0.80,
            MinimumBeatsBeforeEndForJumpDestination = 1
        });

        guard.IsInEndZone(track.Beats[0], index).Should().BeFalse();
        guard.IsInEndZone(track.Beats[4], index).Should().BeTrue();
    }
}
