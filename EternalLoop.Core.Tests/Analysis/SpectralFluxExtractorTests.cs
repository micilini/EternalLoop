using EternalLoop.Contracts.Options;
using EternalLoop.Core.Analysis;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Analysis;

public sealed class SpectralFluxExtractorTests
{
    [Fact]
    public void Extract_Should_KeepFluxLow_ForStationarySineTone()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(
            durationSeconds: 1.0,
            frequency: 440.0);

        var extractor = new NWavesFeatureExtractor();

        var features = extractor.Extract(audio, new FeatureExtractionOptions
        {
            ComputeDeltas = false
        });

        var nonInitialFlux = features.SpectralFlux.Skip(2).ToArray();

        nonInitialFlux.Should().NotBeEmpty();
        nonInitialFlux.Average().Should().BeLessThan(0.20f);
    }

    [Fact]
    public void Extract_Should_CreateFluxPeak_WhenToneStartsAfterSilence()
    {
        var audio = TestSignalFactory.CreateSilenceThenToneLoadedAudio(
            silenceSeconds: 0.5,
            toneSeconds: 0.5,
            frequency: 440.0);

        var extractor = new NWavesFeatureExtractor();

        var options = new FeatureExtractionOptions
        {
            ComputeDeltas = false
        };

        var features = extractor.Extract(audio, options);

        var peakIndex = Array.IndexOf(features.SpectralFlux, features.SpectralFlux.Max());
        var peakSeconds = peakIndex * options.HopLength / (double)audio.SampleRate;

        peakSeconds.Should().BeInRange(0.35, 0.65);
        features.SpectralFlux[peakIndex].Should().BeGreaterThan(0.8f);
    }
}
