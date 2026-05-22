using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.AI;

[Trait("Category", "AI")]
public sealed class AiPreprocessingOnnxCompatibilityTests
{
    private const double TestDurationSeconds = 1.0;

    [Fact]
    public void Preprocessing_outputs_patches_that_can_feed_onnx_model()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: TestDurationSeconds);
        var preprocessor = new AiAudioPreprocessor();
        var melExtractor = new AiMelSpectrogramExtractor(new AiMelFilterBank());
        var patchExtractor = new AiPatchExtractor();
        var batcher = new AiPatchBatcher();
        var modelPathResolver = new AiModelPathResolver();
        var manifestLoader = new AiModelManifestLoader(modelPathResolver);
        using var model = new OnnxMusicEmbeddingModel(
            manifestLoader,
            modelPathResolver,
            NullLogger<OnnxMusicEmbeddingModel>.Instance);

        var samples = preprocessor.ResampleToModelRate(audio, AiPreprocessingDefaultValues.SampleRate);
        var melSpectrogram = melExtractor.Extract(
            samples,
            AiPreprocessingDefaultValues.SampleRate,
            AiPreprocessingDefaultValues.MelBands,
            AiPreprocessingDefaultValues.FftSize,
            AiPreprocessingDefaultValues.HopLength);
        var patches = patchExtractor.ExtractPatches(
            melSpectrogram,
            AiPreprocessingDefaultValues.MelBands,
            AiPreprocessingDefaultValues.PatchFrames,
            AiPreprocessingDefaultValues.PatchHopFrames);
        var batch = batcher.CreateBatches(patches, AiPreprocessingDefaultValues.BatchSize)[0];
        var realPatches = batch.Patches.Take(batch.RealPatchCount).ToArray();

        var embeddings = model.Predict(realPatches);

        embeddings.Should().HaveCount(batch.RealPatchCount);
        embeddings.Should().OnlyContain(embedding => embedding.Length == AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
        embeddings.SelectMany(embedding => embedding).Should().OnlyContain(value => float.IsFinite(value));
    }
}
