using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.BeatTracking;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.BeatTracking;

public sealed class SpectralFluxBeatTrackerTests
{
    [Fact]
    public void Track_Should_ReturnEstimatedBpm_For120BpmSyntheticFlux()
    {
        var audio = TestSignalFactory.CreateClickTrackLoadedAudio(durationSeconds: 12.0, bpm: 120.0);
        var features = CreateFeatureMatrixFromSyntheticFlux(120.0);
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.EstimatedBpm.Should().BeApproximately(120.0, 3.0);
        result.BeatTimes.Length.Should().BeInRange(18, 30);
    }

    [Fact]
    public void Track_Should_ReturnBeatTimes_InAscendingOrder()
    {
        var audio = TestSignalFactory.CreateClickTrackLoadedAudio(durationSeconds: 12.0, bpm: 120.0);
        var features = CreateFeatureMatrixFromSyntheticFlux(120.0);
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.BeatTimes.Zip(result.BeatTimes.Skip(1), (a, b) => b > a).Should().OnlyContain(value => value);
    }

    [Fact]
    public void Track_Should_ReturnConfidences_WithSameLengthAsBeatTimes()
    {
        var audio = TestSignalFactory.CreateClickTrackLoadedAudio(durationSeconds: 12.0, bpm: 120.0);
        var features = CreateFeatureMatrixFromSyntheticFlux(120.0);
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.Confidences.Should().HaveCount(result.BeatTimes.Length);
    }

    [Fact]
    public void Track_Should_ReturnEmptyBeatTimes_WhenSpectralFluxIsEmpty()
    {
        var audio = TestSignalFactory.CreateClickTrackLoadedAudio();
        var features = new FeatureMatrix
        {
            Mfcc = [],
            Chroma = [],
            SpectralFlux = [],
            Rms = [],
            HopLengthSamples = 512,
            FrameSizeSamples = 2048
        };
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.EstimatedBpm.Should().Be(120.0);
        result.BeatTimes.Should().BeEmpty();
        result.Confidences.Should().BeEmpty();
    }

    [Fact]
    public void Track_Should_Throw_WhenAudioIsNull()
    {
        var features = CreateFeatureMatrixFromSyntheticFlux(120.0);
        var tracker = new SpectralFluxBeatTracker();

        var act = () => tracker.Track(null!, features, new BeatTrackingOptions());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Track_Should_Throw_WhenFeaturesAreNull()
    {
        var audio = TestSignalFactory.CreateClickTrackLoadedAudio();
        var tracker = new SpectralFluxBeatTracker();

        var act = () => tracker.Track(audio, null!, new BeatTrackingOptions());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Track_Returns_Reasonable_Beat_Count_For_DrumAndBass_Like_Track()
    {
        const int sampleRate = 22_050;
        const int hop = 512;
        const double durationSeconds = 30.0;
        const double bpm = 136.0;

        var frameCount = (int)Math.Ceiling(durationSeconds * sampleRate / hop);
        var periodFrames = sampleRate / (double)hop * 60.0 / bpm;
        var flux = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame += (int)Math.Round(periodFrames))
        {
            flux[frame] = 0.35f;
        }

        flux[0] = 1f;
        flux[frameCount / 2] = 1f;
        flux[^1] = 1f;

        var audio = new LoadedAudio(
            new float[(int)(sampleRate * durationSeconds)],
            sampleRate,
            durationSeconds,
            "hash");
        var features = new FeatureMatrix
        {
            Mfcc = Enumerable.Range(0, frameCount).Select(_ => new float[13]).ToArray(),
            Chroma = Enumerable.Range(0, frameCount).Select(_ => new float[12]).ToArray(),
            SpectralFlux = flux,
            Rms = new float[frameCount],
            HopLengthSamples = hop,
            FrameSizeSamples = 2048
        };
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.EstimatedBpm.Should().BeInRange(120, 150);
        result.BeatTimes.Length.Should().BeGreaterThan(40);
    }

    private static FeatureMatrix CreateFeatureMatrixFromSyntheticFlux(
        double bpm,
        int sampleRate = 22_050,
        int hopLength = 512,
        int frameSize = 2048,
        double durationSeconds = 12.0)
    {
        var frameCount = (int)(durationSeconds * sampleRate / hopLength);
        var flux = new float[frameCount];
        var framesPerSecond = sampleRate / (double)hopLength;
        var periodFrames = framesPerSecond * 60.0 / bpm;

        for (var frame = 0.0; frame < frameCount; frame += periodFrames)
        {
            var index = (int)Math.Round(frame);

            if (index >= 0 && index < flux.Length)
            {
                flux[index] = 1.0f;
            }
        }

        return new FeatureMatrix
        {
            Mfcc = Enumerable.Range(0, frameCount).Select(_ => new float[13]).ToArray(),
            Chroma = Enumerable.Range(0, frameCount).Select(_ => new float[12]).ToArray(),
            SpectralFlux = flux,
            Rms = new float[frameCount],
            HopLengthSamples = hopLength,
            FrameSizeSamples = frameSize
        };
    }
}
