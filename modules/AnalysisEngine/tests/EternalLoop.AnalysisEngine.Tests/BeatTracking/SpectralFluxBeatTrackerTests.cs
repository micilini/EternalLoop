using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking;

public sealed class SpectralFluxBeatTrackerTests
{
    [Fact]
    public void Track_returns_beats_for_regular_spectral_flux_pulses()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 5.0);
        var features = CreatePulseFeatureMatrix(frameCount: 220, pulseIntervalFrames: 22);
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.BeatTimes.Should().NotBeEmpty();
        result.Confidences.Should().HaveCount(result.BeatTimes.Length);
        result.EstimatedBpm.Should().BeInRange(105.0, 130.0);
    }

    [Fact]
    public void Track_keeps_beat_times_inside_audio_duration()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 5.0);
        var features = CreatePulseFeatureMatrix(frameCount: 220, pulseIntervalFrames: 22);
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.BeatTimes.Should().OnlyContain(time => time >= 0.0 && time <= audio.DurationSeconds);
    }

    [Fact]
    public void Track_falls_back_for_silent_spectral_flux()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 2.0);
        var features = CreateSilentFeatureMatrix(frameCount: 90);
        var tracker = new SpectralFluxBeatTracker();

        var result = tracker.Track(audio, features, new BeatTrackingOptions());

        result.EstimatedBpm.Should().Be(120.0);
        result.BeatTimes.Should().NotBeEmpty();
        result.Confidences.Should().OnlyContain(confidence => confidence > 0.0);
    }

    [Fact]
    public void Track_rejects_invalid_sample_rate()
    {
        var audio = new LoadedAudio(
            [],
            0,
            0.0,
            "hash",
            "C:\\Tests\\invalid.wav",
            "invalid.wav");
        var features = CreatePulseFeatureMatrix(frameCount: 10, pulseIntervalFrames: 2);
        var tracker = new SpectralFluxBeatTracker();

        var act = () => tracker.Track(audio, features, new BeatTrackingOptions());

        act.Should().Throw<ArgumentException>();
    }

    private static FeatureMatrix CreatePulseFeatureMatrix(int frameCount, int pulseIntervalFrames)
    {
        var spectralFlux = new float[frameCount];

        for (var index = 0; index < frameCount; index += pulseIntervalFrames)
        {
            spectralFlux[index] = 1.0f;
        }

        return CreateFeatureMatrix(frameCount, spectralFlux);
    }

    private static FeatureMatrix CreateSilentFeatureMatrix(int frameCount)
    {
        return CreateFeatureMatrix(frameCount, new float[frameCount]);
    }

    private static FeatureMatrix CreateFeatureMatrix(int frameCount, float[] spectralFlux)
    {
        var mfcc = new float[frameCount][];
        var chroma = new float[frameCount][];
        var rms = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            mfcc[frame] = Enumerable.Range(0, 26).Select(index => (float)(frame + index)).ToArray();
            chroma[frame] = Enumerable.Range(0, 12).Select(index => index == frame % 12 ? 1.0f : 0.0f).ToArray();
            rms[frame] = frame / (float)Math.Max(1, frameCount - 1);
        }

        return new FeatureMatrix
        {
            Mfcc = mfcc,
            Chroma = chroma,
            SpectralFlux = spectralFlux,
            Rms = rms,
            HopLengthSamples = 512,
            FrameSizeSamples = 2048,
            SampleRate = TestSignalFactory.DefaultSampleRate
        };
    }
}
