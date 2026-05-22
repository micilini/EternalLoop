using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class BeatMicrosegmentExtractorTests
{
    [Fact]
    public void Extract_Should_ReturnEmpty_WhenNoBeats()
    {
        var result = BeatMicrosegmentExtractor.Extract([], CreateFeatures(), 22_050, 4);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Extract_Should_CreateExpectedMicrosegmentCountPerBeat()
    {
        var result = BeatMicrosegmentExtractor.Extract(CreateBeats(), CreateFeatures(), 22_050, 4);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(fingerprint => fingerprint.Microsegments.Count == 4);
    }

    [Fact]
    public void Extract_Should_UseExistingFeatureMatrix()
    {
        var result = BeatMicrosegmentExtractor.Extract(CreateBeats(), CreateFeatures(), 22_050, 4);

        result[0].Microsegments[0].Timbre.Should().NotBeEmpty();
        result[0].Microsegments[0].Pitches.Should().NotBeEmpty();
        result[0].Microsegments[0].Flux.Should().BeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public void Extract_Should_HandleShortBeats()
    {
        var beats = new[]
        {
            CreateBeat(0, start: 0.0, duration: 0.01)
        };

        var result = BeatMicrosegmentExtractor.Extract(beats, CreateFeatures(), 22_050, 4);

        result[0].Microsegments.Should().HaveCount(4);
    }

    [Fact]
    public void Extract_Should_OutputFiniteVectors()
    {
        var result = BeatMicrosegmentExtractor.Extract(CreateBeats(), CreateFeatures(), 22_050, 4);

        result.SelectMany(fingerprint => fingerprint.Microsegments)
            .Should().OnlyContain(segment =>
                segment.Timbre.All(float.IsFinite) &&
                segment.Pitches.All(float.IsFinite) &&
                segment.Loudness.All(float.IsFinite) &&
                float.IsFinite(segment.Flux));
    }

    [Fact]
    public void Extract_Should_ClampFrameRanges()
    {
        var beats = new[]
        {
            CreateBeat(0, start: 10.0, duration: 0.5)
        };

        var result = BeatMicrosegmentExtractor.Extract(beats, CreateFeatures(), 22_050, 4);

        result[0].Microsegments.Should().HaveCount(4);
        result[0].Microsegments.Should().OnlyContain(segment => segment.Timbre.Length > 0);
    }

    [Fact]
    public void Extract_Should_NormalizeLoudnessVectors()
    {
        var result = BeatMicrosegmentExtractor.Extract(CreateBeats(), CreateFeatures(), 22_050, 4);
        var all = result.SelectMany(fingerprint => fingerprint.Microsegments).ToArray();

        all.Select(segment => segment.Loudness).Should().OnlyContain(vector => vector.Length == 3);
        all.SelectMany(segment => segment.Loudness).Should().OnlyContain(value => Math.Abs(value) < 10f);
    }

    [Fact]
    public void Extract_Should_SetRelativePositionBetweenZeroAndOne()
    {
        var result = BeatMicrosegmentExtractor.Extract(CreateBeats(), CreateFeatures(), 22_050, 4);

        result.SelectMany(fingerprint => fingerprint.Microsegments)
            .Should().OnlyContain(segment => segment.RelativePosition >= 0f && segment.RelativePosition <= 1f);
    }

    [Fact]
    public void Extract_Should_RejectInvalidSampleRate()
    {
        var act = () => BeatMicrosegmentExtractor.Extract(CreateBeats(), CreateFeatures(), 0, 4);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Extract_Should_RejectInvalidMicrosegmentCount()
    {
        var act = () => BeatMicrosegmentExtractor.Extract(
            CreateBeats(),
            CreateFeatures(),
            22_050,
            TuningDefaultValues.MaxMicrosegmentCount + 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static Beat[] CreateBeats()
    {
        return
        [
            CreateBeat(0, 0.0, 0.5),
            CreateBeat(1, 0.5, 0.5)
        ];
    }

    private static Beat CreateBeat(int index, double start, double duration)
    {
        return new Beat
        {
            Index = index,
            Start = start,
            Duration = duration,
            Confidence = 1.0,
            Timbre = [1f],
            Pitches = [1f],
            Loudness = [0f, 0f, 0f],
            BarPosition = [0f, 1f]
        };
    }

    private static FeatureMatrix CreateFeatures()
    {
        return new FeatureMatrix
        {
            Mfcc = Enumerable.Range(0, 16).Select(i => new[] { i + 1f, 1f }).ToArray(),
            Chroma = Enumerable.Range(0, 16).Select(i => new[] { 1f, i + 1f }).ToArray(),
            SpectralFlux = Enumerable.Range(0, 16).Select(i => i / 20f).ToArray(),
            Rms = Enumerable.Range(0, 16).Select(i => i + 1f).ToArray(),
            HopLengthSamples = 11_025,
            FrameSizeSamples = 2_048
        };
    }
}
