using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Analysis;

public sealed class SegmentBuilderTests
{
    [Fact]
    public void Build_compacts_dense_feature_frames()
    {
        var features = CreateFeatureMatrix(frameCount: 90);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments.Should().HaveCountLessThan(90);
        segments.Count.Should().BeInRange(8, 12);
    }

    [Fact]
    public void Build_keeps_segments_when_density_is_already_low()
    {
        var features = CreateFeatureMatrix(frameCount: 8, hopLengthSamples: 4096);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments.Should().HaveCount(8);
    }

    [Fact]
    public void Build_uses_hop_length_for_segment_start()
    {
        var features = CreateFeatureMatrix(frameCount: 2, hopLengthSamples: 4096);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments[1].Start.Should().BeApproximately(4096.0 / TestSignalFactory.DefaultSampleRate, 0.000001);
    }

    [Fact]
    public void Build_uses_frame_size_for_segment_duration()
    {
        var features = CreateFeatureMatrix(frameCount: 1, hopLengthSamples: 4096);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments[0].Duration.Should().BeApproximately(2048.0 / TestSignalFactory.DefaultSampleRate, 0.000001);
    }

    [Fact]
    public void Build_preserves_mfcc_and_chroma_vectors()
    {
        var features = CreateFeatureMatrix(frameCount: 1, hopLengthSamples: 4096);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments[0].Timbre.Should().Equal(features.Mfcc[0]);
        segments[0].Pitches.Should().Equal(features.Chroma[0]);
    }

    [Fact]
    public void Build_uses_rms_as_loudness_values()
    {
        var features = CreateFeatureMatrix(frameCount: 1, hopLengthSamples: 4096);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments[0].LoudnessStart.Should().Be(features.Rms[0]);
        segments[0].LoudnessMax.Should().Be(features.Rms[0]);
    }

    [Fact]
    public void Build_rejects_invalid_sample_rate()
    {
        var features = CreateFeatureMatrix(frameCount: 1);

        var act = () => SegmentBuilder.Build(features, sampleRate: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Build_aggregates_vectors_without_non_finite_values()
    {
        var features = CreateFeatureMatrix(frameCount: 90);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments.SelectMany(segment => segment.Timbre).Should().OnlyContain(value => float.IsFinite(value));
        segments.SelectMany(segment => segment.Pitches).Should().OnlyContain(value => float.IsFinite(value));
    }

    [Fact]
    public void Build_covers_feature_duration_with_valid_segment_durations()
    {
        var features = CreateFeatureMatrix(frameCount: 90);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);

        segments.Should().OnlyContain(segment => segment.Duration > 0.0);
        segments[^1].Start.Should().BeGreaterThan(segments[0].Start);
        (segments[^1].Start + segments[^1].Duration).Should().BeGreaterThan(segments[0].Start + segments[0].Duration);
    }

    [Fact]
    public void Build_with_acoustic_segmentation_places_boundary_near_timbre_change()
    {
        var features = CreateFeatureMatrix(frameCount: 120);
        for (var frame = 60; frame < features.Mfcc.Length; frame++)
        {
            features.Mfcc[frame][0] += 100.0f;
            features.SpectralFlux[frame] = frame == 60 ? 1.0f : 0.0f;
        }

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate, acousticSegmentation: true, evidenceConfidences: true);
        var result = SegmentBuilder.BuildDetailed(features, TestSignalFactory.DefaultSampleRate, acousticSegmentation: true, evidenceConfidences: true);
        var changeTime = 60 * features.HopLengthSamples / (double)TestSignalFactory.DefaultSampleRate;

        segments.Select(segment => segment.Start).Should().Contain(start => Math.Abs(start - changeTime) < 0.25);
        segments.Select(segment => segment.Confidence).Distinct().Should().HaveCountGreaterThan(1);
        result.Mode.Should().Be("novelty");
        result.NoveltyBoundaryRatio.Should().BeGreaterThan(1.0);
    }

    [Fact]
    public void Build_with_degenerate_novelty_preserves_temporal_fallback()
    {
        var features = CreateConstantFeatureMatrix(frameCount: 90);

        var temporal = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate);
        var acoustic = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate, acousticSegmentation: true, evidenceConfidences: true);

        acoustic.Select(segment => segment.Start).Should().Equal(temporal.Select(segment => segment.Start));
        acoustic.Select(segment => segment.Duration).Should().Equal(temporal.Select(segment => segment.Duration));
    }

    [Fact]
    public void Build_with_acoustic_segmentation_handles_very_short_tracks()
    {
        var features = CreateFeatureMatrix(frameCount: 4);

        var segments = SegmentBuilder.Build(features, TestSignalFactory.DefaultSampleRate, acousticSegmentation: true, evidenceConfidences: true);

        segments.Should().NotBeEmpty();
        segments.Should().OnlyContain(segment => segment.Start >= 0.0 && segment.Duration > 0.0);
    }

    private static FeatureMatrix CreateFeatureMatrix(int frameCount, int hopLengthSamples = 512)
    {
        var mfcc = new float[frameCount][];
        var chroma = new float[frameCount][];
        var rms = new float[frameCount];
        var spectralFlux = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            mfcc[frame] = Enumerable.Range(0, 26).Select(index => frame + (float)index).ToArray();
            chroma[frame] = Enumerable.Range(0, 12).Select(index => index == frame % 12 ? 1.0f : 0.0f).ToArray();
            rms[frame] = frame + 0.5f;
            spectralFlux[frame] = frame;
        }

        return new FeatureMatrix
        {
            Mfcc = mfcc,
            Chroma = chroma,
            Rms = rms,
            SpectralFlux = spectralFlux,
            FrameSizeSamples = 2048,
            HopLengthSamples = hopLengthSamples,
            SampleRate = TestSignalFactory.DefaultSampleRate
        };
    }

    private static FeatureMatrix CreateConstantFeatureMatrix(int frameCount)
    {
        var features = CreateFeatureMatrix(frameCount);
        for (var frame = 0; frame < frameCount; frame++)
        {
            Array.Fill(features.Mfcc[frame], 1.0f);
            Array.Fill(features.Chroma[frame], 0.0f);
            features.Chroma[frame][0] = 1.0f;
            features.Rms[frame] = 0.5f;
            features.SpectralFlux[frame] = 0.0f;
        }

        return features;
    }
}
