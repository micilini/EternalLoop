using System.IO;
using System.Windows.Media;
using EternalLoop.App.Services;
using EternalLoop.Core.Diagnostics;
using EternalLoop.App.ViewModels;
using EternalLoop.Core.Runtime;
using EternalLoop.Core.Workflow;
using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Visualization;
using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class PlayerViewModelDisposalTests
{
    [Fact]
    public async Task Dispose_stops_player_and_removes_playback_event_handlers()
    {
        var audioLoader = new FakeAudioLoader();
        var player = new FakeLoopingAudioPlayer();
        var viewModel = CreateViewModel(audioLoader: audioLoader, playerFactory: new FakePlayerFactory(player));

        await viewModel.InitializeAsync();

        player.BeatChangedSubscriberCount.Should().Be(1);
        player.BranchJumpedSubscriberCount.Should().Be(1);
        player.StateChangedSubscriberCount.Should().Be(1);
        player.PlaybackCompletedSubscriberCount.Should().Be(1);

        viewModel.Dispose();

        player.StopCount.Should().Be(1);
        player.DisposeCount.Should().Be(1);
        player.BeatChangedSubscriberCount.Should().Be(0);
        player.BranchJumpedSubscriberCount.Should().Be(0);
        player.StateChangedSubscriberCount.Should().Be(0);
        player.PlaybackCompletedSubscriberCount.Should().Be(0);
    }

    [Fact]
    public void Dispose_clears_artwork_graph_and_playback_status()
    {
        var artworkService = new FakeArtworkService(new DrawingImage());
        var viewModel = CreateViewModel(artworkService: artworkService);

        viewModel.HasTrackArtwork.Should().BeTrue();
        viewModel.Graph.Should().NotBeSameAs(BranchGraph.Empty);

        viewModel.Dispose();

        viewModel.HasTrackArtwork.Should().BeFalse();
        viewModel.TrackArtwork.Should().BeNull();
        viewModel.Graph.Should().BeSameAs(BranchGraph.Empty);
        viewModel.IsPlaying.Should().BeFalse();
        viewModel.LastJumpFromBeat.Should().Be(-1);
        viewModel.LastJumpToBeat.Should().Be(-1);
    }

    internal static PlayerViewModel CreateViewModel(
        ITrackArtworkService? artworkService = null,
        IAudioLoader? audioLoader = null,
        ILoopingAudioPlayerFactory? playerFactory = null,
        IAppLogger? logger = null)
    {
        return new PlayerViewModel(
            CreatePackage(),
            (_, _) => { },
            () => { },
            artworkService ?? new FakeArtworkService(null),
            audioLoader ?? new FakeAudioLoader(),
            playerFactory ?? new FakePlayerFactory(new FakeLoopingAudioPlayer()),
            new BranchGraphBuilder(),
            "Test analysis",
            logger);
    }

    internal static TrackRuntimePackage CreatePackage()
    {
        var first = new RuntimeBeat { Which = 0, Start = 0, Duration = 1, Confidence = 1 };
        var second = new RuntimeBeat { Which = 1, Start = 1, Duration = 1, Confidence = 1 };
        first.Next = second;
        second.Prev = first;
        first.Neighbors.Add(new RuntimeBranchEdge
        {
            Id = 1,
            FromBeat = 0,
            ToBeat = 1,
            JumpBeats = 1,
            Direction = "forward",
            Distance = 0.25,
            SourceBeat = first,
            DestinationBeat = second
        });

        var track = new RuntimeTrack
        {
            Id = "track",
            Title = "Track",
            Artist = "Artist",
            AudioPath = "track.wav",
            AnalysisPath = "analysis.json",
            BranchesPath = "branches.json",
            DurationSeconds = 2,
            Beats = [first, second],
            ActiveBranchCount = 1
        };

        return new TrackRuntimePackage(
            new TrackRuntimeMetadata("track", "Track", "Artist", "hash", 2, 120, 4, "schema", 4, DateTime.UtcNow),
            new TrackRuntimeFileSet(Path.GetTempPath(), "track.wav", "analysis.json", "branches.json"),
            new TrackRuntimeTuningSnapshot("Balanced", 0.78, 2, 16, 6, "beats", 95, true, 0.22, 12, 0.78),
            track,
            new BranchDecisionOptions(),
            new TrackRuntimePreparationSummary(2, 1, true),
            0,
            0);
    }

    internal sealed class FakeArtworkService(ImageSource? image) : ITrackArtworkService
    {
        public string GetDisplayTitle(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        public ImageSource? TryLoadArtwork(string filePath)
        {
            return image;
        }
    }

    internal sealed class FakeAudioLoader : IAudioLoader
    {
        public Task<LoadedAudio> LoadAsync(string audioPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LoadedAudio
            {
                SourcePath = audioPath,
                Samples = [0, 0, 0, 0],
                SampleRate = 2,
                Channels = 1,
                DurationSeconds = 2,
                TotalSampleFrames = 4
            });
        }
    }

    internal class FakePlayerFactory(FakeLoopingAudioPlayer player) : ILoopingAudioPlayerFactory
    {
        public virtual ILoopingAudioPlayer Create(
            LoadedAudio audio,
            RuntimeTrack track,
            BranchDecisionOptions? options = null)
        {
            return player;
        }
    }

    internal sealed class FakeLoopingAudioPlayer : ILoopingAudioPlayer
    {
        private EventHandler<BeatChangedEventArgs>? _beatChanged;
        private EventHandler<BranchJumpEventArgs>? _branchJumped;
        private EventHandler<PlaybackStateChangedEventArgs>? _stateChanged;
        private EventHandler? _playbackCompleted;

        public event EventHandler<BeatChangedEventArgs>? BeatChanged
        {
            add => _beatChanged += value;
            remove => _beatChanged -= value;
        }

        public event EventHandler<BranchJumpEventArgs>? BranchJumped
        {
            add => _branchJumped += value;
            remove => _branchJumped -= value;
        }

        public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged
        {
            add => _stateChanged += value;
            remove => _stateChanged -= value;
        }

        public event EventHandler? PlaybackCompleted
        {
            add => _playbackCompleted += value;
            remove => _playbackCompleted -= value;
        }

        public int BeatChangedSubscriberCount => _beatChanged?.GetInvocationList().Length ?? 0;

        public int BranchJumpedSubscriberCount => _branchJumped?.GetInvocationList().Length ?? 0;

        public int StateChangedSubscriberCount => _stateChanged?.GetInvocationList().Length ?? 0;

        public int PlaybackCompletedSubscriberCount => _playbackCompleted?.GetInvocationList().Length ?? 0;

        public int StopCount { get; private set; }

        public int PauseCount { get; private set; }

        public int PlayCount { get; private set; }

        public int SeekCount { get; private set; }

        public int SetBringItHomeCount { get; private set; }

        public bool BringItHomeEnabled { get; private set; }

        public double LastSeekSeconds { get; private set; }

        public int DisposeCount { get; private set; }

        public Exception? PlayException { get; init; }

        public PlaybackState State { get; private set; }

        public int CurrentBeatIndex { get; private set; }

        public double PositionSeconds { get; private set; }

        public double DurationSeconds => 2;

        public void Play()
        {
            if (PlayException is not null)
            {
                throw PlayException;
            }

            PlayCount++;
            State = PlaybackState.Playing;
            _stateChanged?.Invoke(this, new PlaybackStateChangedEventArgs { State = State });
        }

        public void Pause()
        {
            PauseCount++;
            State = PlaybackState.Paused;
            _stateChanged?.Invoke(this, new PlaybackStateChangedEventArgs { State = State });
        }

        public void Stop()
        {
            StopCount++;
            State = PlaybackState.Stopped;
        }

        public void Seek(double seconds)
        {
            SeekCount++;
            LastSeekSeconds = seconds;
            PositionSeconds = seconds;
        }

        public void SetBringItHome(bool enabled)
        {
            SetBringItHomeCount++;
            BringItHomeEnabled = enabled;
        }

        public void Dispose()
        {
            DisposeCount++;
        }

        public void RaiseBeatChanged(int beatIndex, double positionSeconds)
        {
            CurrentBeatIndex = beatIndex;
            PositionSeconds = positionSeconds;
            _beatChanged?.Invoke(this, new BeatChangedEventArgs
            {
                BeatIndex = beatIndex,
                BeatStartSeconds = positionSeconds,
                BeatDurationSeconds = 1
            });
        }

        public void RaiseBranchJumped(int fromBeatIndex, int toBeatIndex)
        {
            _branchJumped?.Invoke(this, new BranchJumpEventArgs
            {
                FromBeatIndex = fromBeatIndex,
                SeedBeatIndex = fromBeatIndex,
                ToBeatIndex = toBeatIndex,
                BranchId = 1,
                Distance = 0.1,
                ChanceBeforeDecision = 1,
                RandomValue = 0,
                Reason = "Test"
            });
        }

        public void RaisePlaybackCompleted()
        {
            State = PlaybackState.Stopped;
            _playbackCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
