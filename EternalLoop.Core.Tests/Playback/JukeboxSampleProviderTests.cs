using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Playback;
using FluentAssertions;
using NAudio.Wave.SampleProviders;

namespace EternalLoop.Core.Tests.Playback;

public sealed class JukeboxSampleProviderTests
{
    [Fact]
    public void Read_Returns_Requested_Count()
    {
        var provider = new JukeboxSampleProvider(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), new PlaybackOptions());
        var buffer = new float[17];

        var read = provider.Read(buffer, 0, buffer.Length);

        read.Should().Be(17);
    }

    [Fact]
    public void Read_Advances_To_Next_Beat_At_Boundary()
    {
        var provider = new JukeboxSampleProvider(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), new PlaybackOptions());
        var buffer = new float[11];

        provider.Read(buffer, 0, buffer.Length);

        provider.CurrentBeatIndex.Should().Be(1);
    }

    [Fact]
    public void Read_Raises_BeatChanged_When_Beat_Changes()
    {
        var provider = new JukeboxSampleProvider(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), new PlaybackOptions());
        BeatChangedEventArgs? raised = null;
        provider.BeatChanged += (_, args) => raised = args;

        provider.Read(new float[11], 0, 11);

        raised.Should().NotBeNull();
        raised!.BeatIndex.Should().Be(1);
    }

    [Fact]
    public void Read_Uses_Engine_Destination_For_NonLinear_Jump()
    {
        var engine = new FakeJukeboxEngine(CreateBeats(), [2]);
        var provider = new JukeboxSampleProvider(
            CreateAudio(),
            engine,
            new PlaybackOptions { CrossfadeMilliseconds = 0 });
        var buffer = new float[15];

        provider.Read(buffer, 0, buffer.Length);

        buffer.Take(10).Should().OnlyContain(value => Math.Abs(value - 0.1f) < 0.0001f);
        buffer.Skip(10).Should().OnlyContain(value => Math.Abs(value - 0.9f) < 0.0001f);
    }

    [Fact]
    public void Read_Applies_Crossfade_For_NonContiguous_Transition()
    {
        var engine = new FakeJukeboxEngine(CreateBeats(), [2]);
        var provider = new JukeboxSampleProvider(
            CreateAudio(),
            engine,
            new PlaybackOptions { CrossfadeMilliseconds = 200 });
        var buffer = new float[12];

        provider.Read(buffer, 0, buffer.Length);

        buffer[10].Should().NotBeApproximately(0.9f, 0.0001f);
        buffer[11].Should().BeApproximately(0.9f, 0.0001f);
    }

    [Fact]
    public void Reset_Returns_To_First_Beat()
    {
        var provider = new JukeboxSampleProvider(CreateAudio(), new FakeJukeboxEngine(CreateBeats()), new PlaybackOptions());
        provider.Read(new float[12], 0, 12);

        provider.Reset();

        provider.CurrentBeatIndex.Should().Be(0);
        provider.Position.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToWaveProvider_Read_DoesNotThrow_ArrayTypeMismatch()
    {
        var provider = new JukeboxSampleProvider(
            CreateAudio(),
            new FakeJukeboxEngine(CreateBeats()),
            new PlaybackOptions());
        var waveProvider = new SampleToWaveProvider(provider);
        var byteBuffer = new byte[4096];

        var act = () => waveProvider.Read(byteBuffer, 0, byteBuffer.Length);

        act.Should().NotThrow();
    }

    [Fact]
    public void Read_FillsSilence_WhenBeatPointsBeyondSampleBuffer()
    {
        var audio = new LoadedAudio(
            Enumerable.Repeat(0.25f, 10).ToArray(),
            10,
            1.0,
            "hash");
        var beats = new[]
        {
            new Beat
            {
                Index = 0,
                Start = 5.0,
                Duration = 1.0,
                Confidence = 1,
                Timbre = [1f],
                Pitches = [1f],
                Loudness = [0f, 0f, 0f],
                BarPosition = [0f, 1f]
            }
        };
        var provider = new JukeboxSampleProvider(
            audio,
            new FakeJukeboxEngine(beats),
            new PlaybackOptions());
        var buffer = new float[8];

        var act = () => provider.Read(buffer, 0, buffer.Length);

        act.Should().NotThrow();
        buffer.Should().OnlyContain(sample => sample == 0f);
    }

    [Fact]
    public void Seek_UpdatesPositionAndBeat()
    {
        var provider = new JukeboxSampleProvider(
            CreateAudio(),
            new FakeJukeboxEngine(CreateBeats()),
            new PlaybackOptions());
        BeatChangedEventArgs? raised = null;
        provider.BeatChanged += (_, args) => raised = args;

        provider.Seek(TimeSpan.FromSeconds(2.2));

        provider.CurrentBeatIndex.Should().Be(2);
        provider.Position.Should().BeCloseTo(TimeSpan.FromSeconds(2.2), TimeSpan.FromMilliseconds(1));
        raised.Should().NotBeNull();
        raised!.BeatIndex.Should().Be(2);
    }

    [Fact]
    public void Seek_ClearsPendingCrossfade()
    {
        var engine = new FakeJukeboxEngine(CreateBeats(), [2]);
        var provider = new JukeboxSampleProvider(
            CreateAudio(),
            engine,
            new PlaybackOptions { CrossfadeMilliseconds = 200 });
        var beforeSeek = new float[12];
        provider.Read(beforeSeek, 0, beforeSeek.Length);

        provider.Seek(TimeSpan.FromSeconds(1.0));
        var afterSeek = new float[1];
        provider.Read(afterSeek, 0, afterSeek.Length);

        afterSeek[0].Should().BeApproximately(0.2f, 0.0001f);
    }

    private static LoadedAudio CreateAudio()
    {
        var samples = Enumerable.Repeat(0.1f, 10)
            .Concat(Enumerable.Repeat(0.2f, 10))
            .Concat(Enumerable.Repeat(0.9f, 10))
            .Concat(Enumerable.Repeat(-0.4f, 10))
            .ToArray();

        return new LoadedAudio(samples, 10, 4.0, "hash");
    }

    private static Beat[] CreateBeats()
    {
        return Enumerable.Range(0, 4)
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

    private sealed class FakeJukeboxEngine : IJukeboxEngine
    {
        private readonly Queue<int> _nextBeats;
        private int _currentBeatIndex;

        public FakeJukeboxEngine(IReadOnlyList<Beat> beats, IEnumerable<int>? nextBeats = null)
        {
            Beats = beats;
            _nextBeats = new Queue<int>(nextBeats ?? Enumerable.Range(1, Math.Max(0, beats.Count - 1)));
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

        public int PeekNextBeatIndex() => _nextBeats.Count > 0 ? _nextBeats.Peek() : 0;

        public int AdvanceToNextBeat()
        {
            var previous = _currentBeatIndex;
            _currentBeatIndex = _nextBeats.Count > 0 ? _nextBeats.Dequeue() : 0;

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
