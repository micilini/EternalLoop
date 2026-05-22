using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.AI;

[Trait("Category", "AI")]
public sealed class AiEmbeddingExtractorOnnxTests
{
    private const double TestDurationSeconds = 1.0;
    private const double NormTolerance = 0.0001;

    [Fact]
    public async Task ExtractAsync_with_real_onnx_returns_512d_finite_normalized_embeddings()
    {
        var modelPathResolver = new AiModelPathResolver();
        var manifestLoader = new AiModelManifestLoader(modelPathResolver);
        using var model = new OnnxMusicEmbeddingModel(
            manifestLoader,
            modelPathResolver,
            NullLogger<OnnxMusicEmbeddingModel>.Instance);
        var extractor = new AiEmbeddingExtractor(
            new AiAudioPreprocessor(),
            new AiMelSpectrogramExtractor(new AiMelFilterBank()),
            new AiPatchExtractor(),
            new AiPatchBatcher(),
            model,
            manifestLoader);
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: TestDurationSeconds);

        var result = await extractor.ExtractAsync(audio, new ProgressRecorder(), CancellationToken.None);

        result.ModelId.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
        result.ModelVersion.Should().Be(AiModelDefaultValues.DiscogsEffNetVersion);
        result.SampleRate.Should().Be(AiPreprocessingDefaultValues.SampleRate);
        result.EmbeddingDimensions.Should().Be(AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
        result.Frames.Should().NotBeEmpty();
        result.Frames.Should().OnlyContain(frame => frame.Vector.Length == AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
        result.Frames.SelectMany(frame => frame.Vector).Should().OnlyContain(value => float.IsFinite(value));
        foreach (var frame in result.Frames.Where(frame => frame.Vector.Any(value => value != 0.0f)))
        {
            VectorNorm(frame.Vector).Should().BeApproximately(1.0, NormTolerance);
        }
    }

    private static double VectorNorm(float[] vector)
    {
        return Math.Sqrt(vector.Sum(value => (double)value * value));
    }

    private sealed class ProgressRecorder : IAnalysisProgressReporter
    {
        public void Report(AnalysisStage stage, double progress01, string? message = null)
        {
        }
    }
}
