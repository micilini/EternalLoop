using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class PlayerViewModelSeekTests
{
    [Fact]
    public void CommitSeekCommandShouldClampSeekTargetToDuration()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        viewModel.TrackDurationSeconds.Should().Be(2);

        viewModel.CommitSeekCommand.Execute(99d);

        viewModel.PositionSeconds.Should().Be(2);
    }

    [Fact]
    public async Task CommitSeekCommandShouldCallPlayerSeekWhenPlayerExists()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();

        viewModel.CommitSeekCommand.Execute(1.25d);

        player.SeekCount.Should().Be(1);
        player.LastSeekSeconds.Should().Be(1.25);
        viewModel.PositionSeconds.Should().Be(1.25);
    }

    [Fact]
    public async Task BeginSeekCommandShouldKeepBeatChangedFromOverwritingPositionUntilCommit()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();
        viewModel.PositionSeconds = 0.5;

        viewModel.BeginSeekCommand.Execute(null);
        player.RaiseBeatChanged(1, 1.1);

        viewModel.PositionSeconds.Should().Be(0.5);
        viewModel.CurrentBeatIndex.Should().Be(1);
    }
}
