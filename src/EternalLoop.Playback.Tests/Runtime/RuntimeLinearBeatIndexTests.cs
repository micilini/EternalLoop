using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Runtime;

public sealed class RuntimeLinearBeatIndexTests
{
    [Fact]
    public void FromFirstBeatShouldCollectLinearBeatsOnceInOrder()
    {
        RuntimeBeat first = Beat(10);
        RuntimeBeat second = Beat(20);
        RuntimeBeat third = Beat(30);
        first.Next = second;
        second.Next = third;

        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromFirstBeat(first);

        index.Beats.Should().Equal(first, second, third);
        index.Count.Should().Be(3);
    }

    [Fact]
    public void FromFirstBeatShouldStopOnCycle()
    {
        RuntimeBeat first = Beat(0);
        RuntimeBeat second = Beat(1);
        first.Next = second;
        second.Next = first;

        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromFirstBeat(first);

        index.Beats.Should().Equal(first, second);
    }

    [Fact]
    public void ContainsShouldUseReferenceIdentity()
    {
        RuntimeBeat first = Beat(7);
        RuntimeBeat sameWhichDifferentReference = Beat(7);

        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromFirstBeat(first);

        index.Contains(first).Should().BeTrue();
        index.Contains(sameWhichDifferentReference).Should().BeFalse();
    }

    [Fact]
    public void TryGetOrdinalShouldReturnReferenceOrdinal()
    {
        RuntimeBeat first = Beat(100);
        RuntimeBeat second = Beat(50);
        first.Next = second;

        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromFirstBeat(first);

        index.TryGetOrdinal(second, out int ordinal).Should().BeTrue();
        ordinal.Should().Be(1);
    }

    [Fact]
    public void GetOrdinalOrWhichShouldFallbackToWhichWhenBeatIsExternal()
    {
        RuntimeBeat first = Beat(0);
        RuntimeBeat external = Beat(42);

        RuntimeLinearBeatIndex index = RuntimeLinearBeatIndex.FromFirstBeat(first);

        index.GetOrdinalOrWhich(external).Should().Be(42);
    }

    [Fact]
    public void FromTrackShouldRejectTrackWithoutBeats()
    {
        var track = new RuntimeTrack
        {
            Id = "empty",
            Title = "Empty",
            Artist = "Local",
            AudioPath = "empty.wav",
            DurationSeconds = 0,
            Beats = []
        };

        Action act = () => RuntimeLinearBeatIndex.FromTrack(track);

        act.Should().Throw<PlaybackException>().WithMessage("Track must contain beats.");
    }

    private static RuntimeBeat Beat(int which)
    {
        return new RuntimeBeat
        {
            Which = which,
            Start = which,
            Duration = 1,
            Confidence = 1
        };
    }
}
