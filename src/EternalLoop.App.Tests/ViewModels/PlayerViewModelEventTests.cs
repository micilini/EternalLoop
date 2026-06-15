using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class PlayerViewModelEventTests
{
    [Fact]
    public async Task BeatChangedShouldUpdateCurrentBeatIndexWhileAlive()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();

        player.RaiseBeatChanged(1, 1.2);

        viewModel.CurrentBeatIndex.Should().Be(1);
        viewModel.PositionSeconds.Should().Be(1.2);
    }

    [Fact]
    public async Task BranchJumpedShouldUpdateLastJumpIndicesWhileAlive()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();

        player.RaiseBranchJumped(2, 4);

        viewModel.LastJumpFromBeat.Should().Be(2);
        viewModel.LastJumpToBeat.Should().Be(4);
    }

    [Fact]
    public async Task PlaybackEventsRaisedAfterDisposeShouldBeIgnored()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();

        viewModel.Dispose();
        player.RaiseBeatChanged(1, 1.2);
        player.RaiseBranchJumped(1, 3);

        viewModel.CurrentBeatIndex.Should().Be(0);
        viewModel.LastJumpFromBeat.Should().Be(-1);
        viewModel.LastJumpToBeat.Should().Be(-1);
    }
}
