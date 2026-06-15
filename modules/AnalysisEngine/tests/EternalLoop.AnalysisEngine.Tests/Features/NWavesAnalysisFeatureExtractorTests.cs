using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Features;

public sealed class NWavesAnalysisFeatureExtractorTests
{
    [Fact]
    public void Extract_returns_aligned_feature_frames()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        features.Mfcc.Should().NotBeEmpty();
        features.Chroma.Should().NotBeEmpty();
        features.SpectralFlux.Should().NotBeEmpty();
        features.Rms.Should().NotBeEmpty();

        features.Mfcc.Length.Should().Be(features.Chroma.Length);
        features.Mfcc.Length.Should().Be(features.SpectralFlux.Length);
        features.Mfcc.Length.Should().Be(features.Rms.Length);
    }

    [Fact]
    public void Extract_uses_expected_frame_configuration()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        features.FrameSizeSamples.Should().Be(FeatureExtractionOptions.DefaultFrameSize);
        features.HopLengthSamples.Should().Be(FeatureExtractionOptions.DefaultHopLength);
        features.SampleRate.Should().Be(TestSignalFactory.DefaultSampleRate);
    }

    [Fact]
    public void Extract_returns_26_mfcc_values_per_frame_by_default()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        features.Mfcc.Should().AllSatisfy(frame => frame.Should().HaveCount(26));
    }

    [Fact]
    public void Extract_returns_13_mfcc_values_when_deltas_are_disabled()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions
        {
            ComputeDeltas = false
        });

        features.Mfcc.Should().AllSatisfy(frame => frame.Should().HaveCount(13));
    }

    [Fact]
    public void Extract_returns_12_chroma_values_per_frame()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        features.Chroma.Should().AllSatisfy(frame => frame.Should().HaveCount(12));
    }

    [Fact]
    public void Extract_concentrates_chroma_around_a_for_440hz_tone()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(
            durationSeconds: 1.0,
            frequency: 440.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions
        {
            ComputeDeltas = false
        });

        var averageChroma = Average(features.Chroma);
        var strongestPitchClass = Array.IndexOf(averageChroma, averageChroma.Max());

        strongestPitchClass.Should().Be(9);
    }

    [Fact]
    public void Extract_does_not_return_nan_or_infinity()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        AllValues(features).Should().OnlyContain(value => IsFinite(value));
    }

    [Fact]
    public void Extract_handles_silent_audio()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        features.Mfcc.Should().NotBeEmpty();
        features.Chroma.Should().NotBeEmpty();
        features.Rms.Should().NotBeEmpty();
        features.SpectralFlux.Should().NotBeEmpty();
        AllValues(features).Should().OnlyContain(value => IsFinite(value));
    }

    [Fact]
    public void Extract_is_deterministic_for_same_audio()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();
        var options = new FeatureExtractionOptions();

        var first = extractor.Extract(audio, options);
        var second = extractor.Extract(audio, options);

        Flatten(first.Mfcc).Should().Equal(Flatten(second.Mfcc));
        Flatten(first.Chroma).Should().Equal(Flatten(second.Chroma));
        first.Rms.Should().Equal(second.Rms);
        first.SpectralFlux.Should().Equal(second.SpectralFlux);
    }

    [Fact]
    public void Extract_throws_when_samples_are_empty()
    {
        var audio = new LoadedAudio(
            [],
            TestSignalFactory.DefaultSampleRate,
            0.0,
            "test-hash",
            "C:\\Tests\\empty.wav",
            "empty.wav");
        var extractor = new NWavesAnalysisFeatureExtractor();

        var act = () => extractor.Extract(audio, new FeatureExtractionOptions());

        act.Should().Throw<FeatureExtractionException>()
            .WithMessage("*samples*");
    }

    [Fact]
    public void Extract_throws_when_frame_size_is_not_power_of_two()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesAnalysisFeatureExtractor();

        var act = () => extractor.Extract(audio, new FeatureExtractionOptions
        {
            FrameSize = 2000,
            HopLength = 500
        });

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*power of two*");
    }

    private static float[] Average(float[][] vectors)
    {
        vectors.Should().NotBeEmpty();

        var result = new float[vectors[0].Length];

        foreach (var vector in vectors)
        {
            for (var index = 0; index < vector.Length; index++)
            {
                result[index] += vector[index];
            }
        }

        for (var index = 0; index < result.Length; index++)
        {
            result[index] /= vectors.Length;
        }

        return result;
    }

    private static IEnumerable<float> AllValues(FeatureMatrix features)
    {
        return Flatten(features.Mfcc)
            .Concat(Flatten(features.Chroma))
            .Concat(features.Rms)
            .Concat(features.SpectralFlux);
    }

    private static IEnumerable<float> Flatten(float[][] values)
    {
        return values.SelectMany(value => value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
