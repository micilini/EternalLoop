using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Audio;

public sealed class LoopingAudioPlayerDisposalTests
{
    [Fact]
    public void Dispose_does_not_raise_state_changed()
    {
        using var player = new LoopingAudioPlayer(
            PlaybackFixtures.LoadedAudio(),
            PlaybackFixtures.BuildTrack());
        int stateChangedCount = 0;
        player.StateChanged += (_, _) => stateChangedCount++;

        player.Dispose();

        stateChangedCount.Should().Be(0);
        player.State.Should().Be(PlaybackState.Disposed);
    }
}
