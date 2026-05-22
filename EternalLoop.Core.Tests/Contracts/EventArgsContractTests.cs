using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Events;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Contracts;

public sealed class EventArgsContractTests
{
    [Fact]
    public void JumpEventArgs_Should_StoreSourceAndDestination()
    {
        var args = new JumpEventArgs(3, 12);

        args.FromBeat.Should().Be(3);
        args.ToBeat.Should().Be(12);
        args.OccurredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PlaybackStateChangedEventArgs_Should_StoreStateTransition()
    {
        var args = new PlaybackStateChangedEventArgs(PlaybackState.Stopped, PlaybackState.Playing, "Started");

        args.OldState.Should().Be(PlaybackState.Stopped);
        args.NewState.Should().Be(PlaybackState.Playing);
        args.Message.Should().Be("Started");
    }

    [Fact]
    public void BeatChangedEventArgs_Should_StoreBeatTiming()
    {
        var args = new BeatChangedEventArgs(8, 4.25, 0.5);

        args.BeatIndex.Should().Be(8);
        args.BeatStart.Should().Be(4.25);
        args.BeatDuration.Should().Be(0.5);
    }
}
