using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Playback;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using AppPlaybackState = EternalLoop.Contracts.Enums.PlaybackState;
using NAudioPlaybackState = NAudio.Wave.PlaybackState;

namespace EternalLoop.Core.Tests.Playback;

public sealed class NAudioStreamPlayerTests
{
    [Fact]
    public async Task LoadAsync_Creates_Output_And_Provider()
    {
        var fakeOutput = new FakeWavePlayer();
        var player = CreatePlayer(fakeOutput);

        await player.LoadAsync(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), CancellationToken.None);

        fakeOutput.InitializedProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task Play_Raises_StateChanged_To_Playing()
    {
        var fakeOutput = new FakeWavePlayer();
        var player = CreatePlayer(fakeOutput);
        var states = new List<AppPlaybackState>();
        player.StateChanged += (_, args) => states.Add(args.NewState);
        await player.LoadAsync(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), CancellationToken.None);

        player.Play();

        fakeOutput.PlaybackState.Should().Be(NAudioPlaybackState.Playing);
        states.Should().Contain(AppPlaybackState.Playing);
    }

    [Fact]
    public async Task Pause_Raises_StateChanged_To_Paused()
    {
        var fakeOutput = new FakeWavePlayer();
        var player = CreatePlayer(fakeOutput);
        var states = new List<AppPlaybackState>();
        player.StateChanged += (_, args) => states.Add(args.NewState);
        await player.LoadAsync(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), CancellationToken.None);
        player.Play();

        player.Pause();

        fakeOutput.PlaybackState.Should().Be(NAudioPlaybackState.Paused);
        states.Should().Contain(AppPlaybackState.Paused);
    }

    [Fact]
    public async Task Stop_Raises_StateChanged_To_Stopped_And_Resets_Position()
    {
        var fakeOutput = new FakeWavePlayer();
        var player = CreatePlayer(fakeOutput);
        var states = new List<AppPlaybackState>();
        player.StateChanged += (_, args) => states.Add(args.NewState);
        await player.LoadAsync(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), CancellationToken.None);
        player.Play();

        player.Stop();

        fakeOutput.PlaybackState.Should().Be(NAudioPlaybackState.Stopped);
        player.Position.Should().Be(TimeSpan.Zero);
        states.Should().Contain(AppPlaybackState.Stopped);
    }

    [Fact]
    public async Task Volume_Is_Clamped_Between_Zero_And_One()
    {
        var fakeOutput = new FakeWavePlayer();
        var player = CreatePlayer(fakeOutput);
        await player.LoadAsync(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), CancellationToken.None);

        player.Volume = 2f;
        player.Volume.Should().Be(1f);
        fakeOutput.Volume.Should().Be(1f);

        player.Volume = -1f;
        player.Volume.Should().Be(0f);
        fakeOutput.Volume.Should().Be(0f);
    }

    [Fact]
    public void Play_Throws_When_Not_Loaded()
    {
        var player = CreatePlayer(new FakeWavePlayer());

        FluentActions.Invoking(player.Play).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Seek_Updates_Position_WhenLoaded()
    {
        var player = CreatePlayer(new FakeWavePlayer());
        await player.LoadAsync(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), CancellationToken.None);

        player.Seek(TimeSpan.FromSeconds(1.0));

        player.Position.Should().BeCloseTo(TimeSpan.FromSeconds(1.0), TimeSpan.FromMilliseconds(1));
    }

    private static NAudioStreamPlayer CreatePlayer(FakeWavePlayer fakeOutput)
    {
        return new NAudioStreamPlayer(
            Options.Create(new PlaybackOptions()),
            NullLogger<NAudioStreamPlayer>.Instance,
            () => fakeOutput);
    }

    private static LoadedAudio CreateAudio()
    {
        var samples = Enumerable.Repeat(0.1f, 20).ToArray();
        return new LoadedAudio(samples, 10, 2.0, "hash");
    }

    private static Beat[] CreateBeats()
    {
        return Enumerable.Range(0, 2)
            .Select(i => new Beat
            {
                Index = i,
                Start = i,
                Duration = 1,
                Confidence = 1,
                Timbre = [1f],
                Pitches = [1f],
                Loudness = [0f, 0f, 0f],
                BarPosition = [0f, 1f]
            })
            .ToArray();
    }

    private sealed class FakeWavePlayer : IWavePlayer
    {
        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public NAudioPlaybackState PlaybackState { get; private set; } = NAudioPlaybackState.Stopped;

        public float Volume { get; set; } = 1f;

        public IWaveProvider? InitializedProvider { get; private set; }

        public WaveFormat OutputWaveFormat => InitializedProvider?.WaveFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(10, 1);

        public void Init(IWaveProvider waveProvider)
        {
            InitializedProvider = waveProvider;
        }

        public void Play()
        {
            PlaybackState = NAudioPlaybackState.Playing;
        }

        public void Pause()
        {
            PlaybackState = NAudioPlaybackState.Paused;
        }

        public void Stop()
        {
            PlaybackState = NAudioPlaybackState.Stopped;
            PlaybackStopped?.Invoke(this, new StoppedEventArgs());
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeJukeboxEngine : IJukeboxEngine
    {
        private int _currentBeatIndex;

        public FakeJukeboxEngine(IReadOnlyList<Beat> beats)
        {
            Beats = beats;
        }

        public event EventHandler<JumpEventArgs>? JumpOccurred;

        public IReadOnlyList<Beat> Beats { get; }

        public void Load(TrackAnalysis analysis, JukeboxGraph graph)
        {
        }

        public void ReloadGraph(JukeboxGraph graph)
        {
        }

        public void UpdateOptions(JukeboxEngineOptions options)
        {
        }

        public void SeekToBeat(int beatIndex)
        {
            _currentBeatIndex = Math.Clamp(beatIndex, 0, Beats.Count - 1);
        }

        public int GetCurrentBeatIndex() => _currentBeatIndex;

        public int PeekNextBeatIndex() => _currentBeatIndex + 1 >= Beats.Count ? 0 : _currentBeatIndex + 1;

        public int AdvanceToNextBeat()
        {
            var previous = _currentBeatIndex;
            _currentBeatIndex = PeekNextBeatIndex();

            if (_currentBeatIndex != previous + 1)
            {
                JumpOccurred?.Invoke(this, new JumpEventArgs(previous, _currentBeatIndex));
            }

            return _currentBeatIndex;
        }

        public void Reset()
        {
            _currentBeatIndex = 0;
        }
    }
}
