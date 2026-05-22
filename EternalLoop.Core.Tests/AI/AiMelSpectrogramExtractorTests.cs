using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiMelSpectrogramExtractorTests
{
    private const double TestDurationSeconds = 1.0;

    [Fact]
    public void Extract_returns_frames_with_128_mel_bands()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(AiPreprocessingDefaultValues.SampleRate, TestDurationSeconds);
        var extractor = CreateExtractor();

        var spectrogram = ExtractDefault(extractor, audio.Samples);

        spectrogram.Should().NotBeEmpty();
        spectrogram.Should().OnlyContain(frame => frame.Length == AiPreprocessingDefaultValues.MelBands);
    }

    [Fact]
    public void Extract_contains_no_nan_or_infinity()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(AiPreprocessingDefaultValues.SampleRate, TestDurationSeconds);
        var extractor = CreateExtractor();

        var spectrogram = ExtractDefault(extractor, audio.Samples);

        spectrogram.SelectMany(frame => frame).Should().OnlyContain(value => float.IsFinite(value));
    }

    [Fact]
    public void Extract_is_deterministic_for_same_audio()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(AiPreprocessingDefaultValues.SampleRate, TestDurationSeconds);
        var extractor = CreateExtractor();

        var first = ExtractDefault(extractor, audio.Samples);
        var second = ExtractDefault(extractor, audio.Samples);

        second.SelectMany(frame => frame).Should().Equal(first.SelectMany(frame => frame));
    }

    [Fact]
    public void Extract_handles_silent_audio_without_crashing()
    {
        var samples = new float[AiPreprocessingDefaultValues.SampleRate];
        var extractor = CreateExtractor();

        var spectrogram = ExtractDefault(extractor, samples);

        spectrogram.Should().NotBeEmpty();
        spectrogram.SelectMany(frame => frame).Should().OnlyContain(value => float.IsFinite(value));
    }

    [Fact]
    public void Extract_returns_empty_for_empty_audio()
    {
        var extractor = CreateExtractor();

        var spectrogram = ExtractDefault(extractor, []);

        spectrogram.Should().BeEmpty();
    }

    private static AiMelSpectrogramExtractor CreateExtractor()
    {
        return new AiMelSpectrogramExtractor(new AiMelFilterBank());
    }

    private static float[][] ExtractDefault(AiMelSpectrogramExtractor extractor, float[] samples)
    {
        return extractor.Extract(
            samples,
            AiPreprocessingDefaultValues.SampleRate,
            AiPreprocessingDefaultValues.MelBands,
            AiPreprocessingDefaultValues.FftSize,
            AiPreprocessingDefaultValues.HopLength);
    }
}
