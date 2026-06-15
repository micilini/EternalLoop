using EternalLoop.App.Tests.TestDoubles;
using EternalLoop.Core.Runtime;
using FluentAssertions;
using System.IO;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class PlayerViewModelPlaybackTests
{
    [Fact]
    public async Task InitializeAsyncShouldCreatePlayerOnce()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var factory = new CountingPlayerFactory(player);
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(playerFactory: factory);

        await viewModel.InitializeAsync();
        await viewModel.InitializeAsync();

        factory.CreateCount.Should().Be(1);
    }

    [Fact]
    public async Task PlayPauseCommandShouldInitializeAndStartPlaybackWhenStopped()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));

        viewModel.PlayPauseCommand.Execute(null);

        await AsyncTest.EventuallyAsync(() =>
        {
            player.PlayCount.Should().Be(1);
            viewModel.IsPlaying.Should().BeTrue();
            viewModel.PlayPauseText.Should().Be("Pause");
        });
    }

    [Fact]
    public async Task PlayPauseCommandShouldPauseWhenAlreadyPlaying()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        viewModel.PlayPauseCommand.Execute(null);
        await AsyncTest.EventuallyAsync(() => viewModel.IsPlaying.Should().BeTrue());

        viewModel.PlayPauseCommand.Execute(null);

        await AsyncTest.EventuallyAsync(() =>
        {
            player.PauseCount.Should().Be(1);
            viewModel.IsPlaying.Should().BeFalse();
            viewModel.PlayPauseText.Should().Be("Play");
        });
    }

    [Fact]
    public async Task StopCommandShouldStopPlayerAndResetPosition()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();
        player.RaiseBeatChanged(1, 1.2);

        viewModel.StopCommand.Execute(null);

        player.StopCount.Should().Be(1);
        viewModel.PositionSeconds.Should().Be(0);
        viewModel.CurrentBeatIndex.Should().Be(0);
        viewModel.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task BringItHomeCommandShouldToggleStateAndTooltip()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();

        viewModel.IsBringItHomeEnabled.Should().BeFalse();
        viewModel.BringItHomeToolTip.Should().Contain("OFF");
        viewModel.BringItHomeStatusText.Should().Be("Finish mode: OFF");

        viewModel.BringItHomeCommand.Execute(null);

        viewModel.IsBringItHomeEnabled.Should().BeTrue();
        viewModel.BringItHomeToolTip.Should().Contain("ON");
        viewModel.BringItHomeStatusText.Should().Be("Finish mode: ON");
        player.BringItHomeEnabled.Should().BeTrue();

        viewModel.BringItHomeCommand.Execute(null);

        viewModel.IsBringItHomeEnabled.Should().BeFalse();
        viewModel.BringItHomeToolTip.Should().Contain("OFF");
        viewModel.BringItHomeStatusText.Should().Be("Finish mode: OFF");
        player.BringItHomeEnabled.Should().BeFalse();
        player.SetBringItHomeCount.Should().Be(2);
    }

    [Fact]
    public async Task StopCommandShouldResetBringItHomeStatus()
    {
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer();
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player));
        await viewModel.InitializeAsync();
        viewModel.BringItHomeCommand.Execute(null);

        viewModel.StopCommand.Execute(null);

        viewModel.IsBringItHomeEnabled.Should().BeFalse();
        viewModel.BringItHomeStatusText.Should().Be("Finish mode: OFF");
    }

    [Fact]
    public async Task AnalyzeAgainCommandShouldUseOriginalPathAndForceReanalysis()
    {
        string root = Directory.CreateTempSubdirectory("eternalloop-player-tests-").FullName;
        string audioPath = Path.Combine(root, "track.wav");
        await File.WriteAllBytesAsync(audioPath, [0]);
        TrackRuntimePackage package = PlayerViewModelDisposalTests.CreatePackage() with
        {
            Files = new TrackRuntimeFileSet(root, audioPath, "analysis.json", "branches.json")
        };
        string? requestedPath = null;
        bool? requestedForce = null;
        var viewModel = new EternalLoop.App.ViewModels.PlayerViewModel(
            package,
            (path, force) =>
            {
                requestedPath = path;
                requestedForce = force;
            },
            () => { },
            new PlayerViewModelDisposalTests.FakeArtworkService(null),
            new PlayerViewModelDisposalTests.FakeAudioLoader(),
            new PlayerViewModelDisposalTests.FakePlayerFactory(new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer()),
            new EternalLoop.Playback.Visualization.BranchGraphBuilder(),
            "Test");

        viewModel.AnalyzeAgainCommand.Execute(null);

        requestedPath.Should().Be(audioPath);
        requestedForce.Should().BeTrue();
        Directory.Delete(root, recursive: true);
    }

    private sealed class CountingPlayerFactory(PlayerViewModelDisposalTests.FakeLoopingAudioPlayer player)
        : PlayerViewModelDisposalTests.FakePlayerFactory(player)
    {
        public int CreateCount { get; private set; }

        public override EternalLoop.Playback.Audio.ILoopingAudioPlayer Create(
            EternalLoop.Playback.Audio.LoadedAudio audio,
            EternalLoop.Playback.Models.RuntimeTrack track,
            EternalLoop.Playback.Runtime.BranchDecisionOptions? options = null)
        {
            CreateCount++;
            return base.Create(audio, track, options);
        }
    }
}
