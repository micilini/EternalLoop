using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class BeatThisPreprocessorTests
{
    [Fact]
    public void Preprocess_returns_expected_tensor_shape_and_data_length()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var preprocessor = new BeatThisPreprocessor();

        var tensor = preprocessor.Preprocess(audio);

        tensor.Shape.Should().Equal(1, 1_500, 128);
        tensor.Data.Should().HaveCount(1_500 * 128);
        tensor.ValidFrameCount.Should().BeGreaterThan(0);
        tensor.ValidFrameCount.Should().BeLessThan(tensor.ChunkFrames);
        tensor.SampleRate.Should().Be(22_050);
        tensor.FrameRate.Should().Be(50.0);
        tensor.MelBins.Should().Be(128);
    }

    [Fact]
    public void Preprocess_outputs_only_finite_values_for_sine_audio()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 1.0);
        var preprocessor = new BeatThisPreprocessor();

        var tensor = preprocessor.Preprocess(audio);

        tensor.Data.Should().OnlyContain(value => !float.IsNaN(value) && !float.IsInfinity(value));
    }

    [Fact]
    public void Preprocess_keeps_padding_zero_after_valid_frames()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 0.25);
        var preprocessor = new BeatThisPreprocessor();

        var tensor = preprocessor.Preprocess(audio);
        var firstPaddingOffset = tensor.ValidFrameCount * tensor.MelBins;

        firstPaddingOffset.Should().BeLessThan(tensor.Data.Length);
        tensor.Data.Skip(firstPaddingOffset).Should().OnlyContain(value => value == 0.0f);
    }

    [Fact]
    public void Preprocess_silent_audio_is_finite_and_stable()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 1.0);
        var preprocessor = new BeatThisPreprocessor();

        var tensor = preprocessor.Preprocess(audio);

        tensor.ValidFrameCount.Should().BeGreaterThan(0);
        tensor.Data.Should().OnlyContain(value => !float.IsNaN(value) && !float.IsInfinity(value));
    }

    [Fact]
    public void Preprocess_rejects_sample_rate_mismatch()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(sampleRate: 44_100);
        var preprocessor = new BeatThisPreprocessor();

        var act = () => preprocessor.Preprocess(audio);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*expected sample rate 22050 Hz*");
    }

    [Fact]
    public void Preprocess_with_custom_options_uses_custom_shape()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 0.5);
        var preprocessor = new BeatThisPreprocessor(new BeatThisPreprocessorOptions
        {
            ChunkFrames = 64,
            MelBins = 32,
            FrameSize = 512,
            Normalize = false
        });

        var tensor = preprocessor.Preprocess(audio);

        tensor.Shape.Should().Equal(1, 64, 32);
        tensor.Data.Should().HaveCount(64 * 32);
        tensor.ChunkFrames.Should().Be(64);
        tensor.MelBins.Should().Be(32);
        tensor.FrameSize.Should().Be(512);
    }

    [Fact]
    public void GetOffset_returns_row_major_offset()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 0.5);
        var preprocessor = new BeatThisPreprocessor(new BeatThisPreprocessorOptions
        {
            ChunkFrames = 64,
            MelBins = 32,
            FrameSize = 512
        });

        var tensor = preprocessor.Preprocess(audio);

        tensor.GetOffset(0, 0).Should().Be(0);
        tensor.GetOffset(1, 0).Should().Be(32);
        tensor.GetOffset(1, 7).Should().Be(39);
    }

    [Fact]
    public void PreprocessChunks_splits_audio_longer_than_one_chunk()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 70.0);
        var preprocessor = new BeatThisPreprocessor();

        var chunks = preprocessor.PreprocessChunks(audio);

        chunks.Should().HaveCountGreaterThan(1);
        chunks[0].StartFrameIndex.Should().Be(0);
        chunks[0].StartTimeSeconds.Should().Be(0.0);
        chunks[1].StartFrameIndex.Should().Be(chunks[0].ChunkFrames);
        chunks[1].StartTimeSeconds.Should().BeApproximately(30.0, 1e-9);
    }

    [Fact]
    public void Preprocess_keeps_returning_first_chunk_for_compatibility()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 70.0);
        var preprocessor = new BeatThisPreprocessor();

        var tensor = preprocessor.Preprocess(audio);

        tensor.StartFrameIndex.Should().Be(0);
        tensor.StartTimeSeconds.Should().Be(0.0);
        tensor.Shape.Should().Equal(1, 1_500, 128);
    }
}
