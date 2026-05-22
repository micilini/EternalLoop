using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiAudioPreprocessorTests
{
    private const int SourceSampleRate = 22_050;
    private const double OneSecond = 1.0;
    private const double TwoSeconds = 2.0;

    [Fact]
    public void ResampleToModelRate_outputs_16000_samples_per_second()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(SourceSampleRate, OneSecond);
        var preprocessor = new AiAudioPreprocessor();

        var samples = preprocessor.ResampleToModelRate(audio, AiPreprocessingDefaultValues.SampleRate);

        samples.Should().HaveCount(AiPreprocessingDefaultValues.SampleRate);
    }

    [Fact]
    public void ResampleToModelRate_returns_copy_when_sample_rate_already_matches()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(AiPreprocessingDefaultValues.SampleRate, OneSecond);
        var preprocessor = new AiAudioPreprocessor();

        var samples = preprocessor.ResampleToModelRate(audio, AiPreprocessingDefaultValues.SampleRate);

        samples.Should().Equal(audio.Samples);
        samples.Should().NotBeSameAs(audio.Samples);
    }

    [Fact]
    public void ResampleToModelRate_preserves_duration_approximately()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(SourceSampleRate, TwoSeconds);
        var preprocessor = new AiAudioPreprocessor();

        var samples = preprocessor.ResampleToModelRate(audio, AiPreprocessingDefaultValues.SampleRate);
        var durationSeconds = samples.Length / (double)AiPreprocessingDefaultValues.SampleRate;

        durationSeconds.Should().BeApproximately(TwoSeconds, 1.0 / AiPreprocessingDefaultValues.SampleRate);
    }

    [Fact]
    public void ResampleToModelRate_outputs_no_nan_or_infinity()
    {
        var audio = new LoadedAudio(
            [0.0f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, 1.0f],
            SourceSampleRate,
            OneSecond,
            "non-finite");
        var preprocessor = new AiAudioPreprocessor();

        var samples = preprocessor.ResampleToModelRate(audio, AiPreprocessingDefaultValues.SampleRate);

        samples.Should().OnlyContain(sample => float.IsFinite(sample));
    }

    [Fact]
    public void ResampleToModelRate_does_not_mutate_input_samples()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(SourceSampleRate, OneSecond);
        var original = audio.Samples.ToArray();
        var preprocessor = new AiAudioPreprocessor();

        _ = preprocessor.ResampleToModelRate(audio, AiPreprocessingDefaultValues.SampleRate);

        audio.Samples.Should().Equal(original);
    }
}
