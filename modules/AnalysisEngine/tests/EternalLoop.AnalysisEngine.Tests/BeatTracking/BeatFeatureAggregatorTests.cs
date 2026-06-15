using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking;

public sealed class BeatFeatureAggregatorTests
{
    [Fact]
    public void AggregateFeatures_creates_one_beat_per_tracked_time()
    {
        var trackingResult = CreateBeatTrackingResult();
        var features = CreateFeatureMatrix();
        var beats = BeatFeatureAggregator.AggregateFeatures(
            trackingResult,
            features,
            TestSignalFactory.DefaultSampleRate,
            timeSignature: 4);

        beats.Should().HaveCount(trackingResult.BeatTimes.Length);
    }

    [Fact]
    public void AggregateFeatures_adds_timbre_pitches_loudness_and_bar_position()
    {
        var trackingResult = CreateBeatTrackingResult();
        var features = CreateFeatureMatrix();
        var beats = BeatFeatureAggregator.AggregateFeatures(
            trackingResult,
            features,
            TestSignalFactory.DefaultSampleRate,
            timeSignature: 4);

        beats.Should().AllSatisfy(beat =>
        {
            beat.Timbre.Should().HaveCount(26);
            beat.Pitches.Should().HaveCount(12);
            beat.Loudness.Should().HaveCount(3);
            beat.BarPosition.Should().HaveCount(2);
        });
    }

    [Fact]
    public void AggregateFeatures_does_not_create_negative_durations()
    {
        var trackingResult = CreateBeatTrackingResult();
        var features = CreateFeatureMatrix();
        var beats = BeatFeatureAggregator.AggregateFeatures(
            trackingResult,
            features,
            TestSignalFactory.DefaultSampleRate,
            timeSignature: 4);

        beats.Should().OnlyContain(beat => beat.Duration > 0.0);
    }

    [Fact]
    public void AggregateFeatures_preserves_beat_order()
    {
        var trackingResult = CreateBeatTrackingResult();
        var features = CreateFeatureMatrix();
        var beats = BeatFeatureAggregator.AggregateFeatures(
            trackingResult,
            features,
            TestSignalFactory.DefaultSampleRate,
            timeSignature: 4);

        beats.Select(beat => beat.Index).Should().Equal(0, 1, 2, 3);
        beats.Select(beat => beat.Start).Should().BeInAscendingOrder();
    }

    private static BeatTrackingResult CreateBeatTrackingResult()
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.8, 0.9, 0.7, 0.6]
        };
    }

    private static FeatureMatrix CreateFeatureMatrix()
    {
        const int frameCount = 100;

        var mfcc = new float[frameCount][];
        var chroma = new float[frameCount][];
        var spectralFlux = new float[frameCount];
        var rms = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            mfcc[frame] = Enumerable.Range(0, 26).Select(index => frame + index * 0.01f).ToArray();
            chroma[frame] = Enumerable.Range(0, 12).Select(index => index == frame % 12 ? 1.0f : 0.0f).ToArray();
            spectralFlux[frame] = frame % 10 == 0 ? 1.0f : 0.0f;
            rms[frame] = frame / (float)(frameCount - 1);
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
