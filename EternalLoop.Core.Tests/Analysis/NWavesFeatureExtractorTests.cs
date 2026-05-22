using EternalLoop.Contracts.Options;
using EternalLoop.Core.Analysis;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Analysis;

public sealed class NWavesFeatureExtractorTests
{
    [Fact]
    public void Extract_Should_ProduceFeatureMatrix_WithAlignedFrameCounts()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions());

        features.Mfcc.Should().NotBeEmpty();
        features.Chroma.Should().NotBeEmpty();
        features.SpectralFlux.Should().NotBeEmpty();

        features.Mfcc.Length.Should().Be(features.Chroma.Length);
        features.Mfcc.Length.Should().Be(features.SpectralFlux.Length);

        features.HopLengthSamples.Should().Be(512);
        features.FrameSizeSamples.Should().Be(2048);
    }

    [Fact]
    public void Extract_Should_AppendDeltas_WhenComputeDeltasIsTrue()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions
        {
            MfccCount = 13,
            ComputeDeltas = true
        });

        features.Mfcc[0].Should().HaveCount(26);
    }

    [Fact]
    public void Extract_Should_NotAppendDeltas_WhenComputeDeltasIsFalse()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var extractor = new NWavesFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions
        {
            MfccCount = 13,
            ComputeDeltas = false
        });

        features.Mfcc[0].Should().HaveCount(13);
    }

    [Fact]
    public void Extract_Should_Throw_WhenSamplesAreEmpty()
    {
        var audio = new EternalLoop.Contracts.Models.LoadedAudio(
            Array.Empty<float>(),
            22_050,
            0,
            "empty");

        var extractor = new NWavesFeatureExtractor();

        var act = () => extractor.Extract(audio, new FeatureExtractionOptions());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*samples*");
    }

    [Fact]
    public void Extract_Should_Throw_WhenFrameSizeIsNotPowerOfTwo()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio();
        var extractor = new NWavesFeatureExtractor();

        var act = () => extractor.Extract(audio, new FeatureExtractionOptions
        {
            FrameSize = 2000,
            HopLength = 500
        });

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*power of two*");
    }
}
