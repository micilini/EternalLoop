using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiPatchExtractorTests
{
    private const int TestMelBands = 3;
    private const int TestPatchFrames = 4;
    private const int TestPatchHopFrames = 2;
    private const int TestFrameCount = 5;

    [Fact]
    public void ExtractPatches_creates_128_by_96_patches()
    {
        var extractor = new AiPatchExtractor();
        var spectrogram = CreateSpectrogram(AiPreprocessingDefaultValues.PatchFrames, AiPreprocessingDefaultValues.MelBands);

        var patches = extractor.ExtractPatches(
            spectrogram,
            AiPreprocessingDefaultValues.MelBands,
            AiPreprocessingDefaultValues.PatchFrames,
            AiPreprocessingDefaultValues.PatchHopFrames);

        patches.Should().NotBeEmpty();
        patches.Should().OnlyContain(patch =>
            patch.Length == AiPreprocessingDefaultValues.MelBands
            && patch.All(band => band.Length == AiPreprocessingDefaultValues.PatchFrames));
    }

    [Fact]
    public void ExtractPatches_transposes_frame_major_mel_to_model_shape()
    {
        var extractor = new AiPatchExtractor();
        var spectrogram = CreateSpectrogram(TestFrameCount, TestMelBands);

        var patch = extractor.ExtractPatches(spectrogram, TestMelBands, TestPatchFrames, TestPatchHopFrames)[0];

        patch[0][0].Should().Be(spectrogram[0][0]);
        patch[1][0].Should().Be(spectrogram[0][1]);
        patch[2][3].Should().Be(spectrogram[3][2]);
    }

    [Fact]
    public void ExtractPatches_pads_short_spectrogram_by_repeating_last_frame()
    {
        var extractor = new AiPatchExtractor();
        var spectrogram = CreateSpectrogram(TestPatchFrames - 1, TestMelBands);

        var patch = extractor.ExtractPatches(spectrogram, TestMelBands, TestPatchFrames, TestPatchHopFrames)[0];

        patch[0][TestPatchFrames - 1].Should().Be(spectrogram[^1][0]);
        patch[1][TestPatchFrames - 1].Should().Be(spectrogram[^1][1]);
    }

    [Fact]
    public void ExtractPatches_returns_empty_for_empty_spectrogram()
    {
        var extractor = new AiPatchExtractor();

        var patches = extractor.ExtractPatches([], TestMelBands, TestPatchFrames, TestPatchHopFrames);

        patches.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPatches_rejects_wrong_mel_band_count()
    {
        var extractor = new AiPatchExtractor();
        IReadOnlyList<float[]> spectrogram = [new float[TestMelBands - 1]];

        var act = () => extractor.ExtractPatches(spectrogram, TestMelBands, TestPatchFrames, TestPatchHopFrames);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExtractPatches_contains_no_nan_or_infinity()
    {
        var extractor = new AiPatchExtractor();
        IReadOnlyList<float[]> spectrogram =
        [
            [float.NaN, float.PositiveInfinity, float.NegativeInfinity],
            [1.0f, 2.0f, 3.0f]
        ];

        var patches = extractor.ExtractPatches(spectrogram, TestMelBands, TestPatchFrames, TestPatchHopFrames);

        patches.SelectMany(patch => patch.SelectMany(band => band)).Should().OnlyContain(value => float.IsFinite(value));
    }

    private static float[][] CreateSpectrogram(int frameCount, int melBands)
    {
        return Enumerable.Range(0, frameCount)
            .Select(frameIndex => Enumerable.Range(0, melBands)
                .Select(melBandIndex => frameIndex * melBands + melBandIndex + 1.0f)
                .ToArray())
            .ToArray();
    }
}
