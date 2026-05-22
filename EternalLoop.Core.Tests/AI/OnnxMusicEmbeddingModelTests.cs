using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.AI;

[Trait("Category", "AI")]
public sealed class OnnxMusicEmbeddingModelTests : IClassFixture<OnnxMusicEmbeddingModelTests.OnnxModelFixture>
{
    private const float FirstPatchValue = 0.001f;
    private const float SecondPatchValue = 0.002f;
    private const int SinglePatchCount = 1;
    private const int TwoPatchCount = 2;
    private const int FullBatchPatchCount = AiModelDefaultValues.DiscogsEffNetBatchSize;
    private const int TooManyPatchCount = AiModelDefaultValues.DiscogsEffNetBatchSize + 1;
    private const int WrongMelBandCount = AiModelDefaultValues.DiscogsEffNetMelBands - 1;

    private readonly OnnxModelFixture _fixture;

    public OnnxMusicEmbeddingModelTests(OnnxModelFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Onnx_session_loads_from_packaged_model()
    {
        _fixture.Model.ModelId.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
    }

    [Fact]
    public void Model_reports_expected_input_shape()
    {
        _fixture.Model.BatchSize.Should().Be(AiModelDefaultValues.DiscogsEffNetBatchSize);
        _fixture.Model.MelBands.Should().Be(AiModelDefaultValues.DiscogsEffNetMelBands);
        _fixture.Model.PatchFrames.Should().Be(AiModelDefaultValues.DiscogsEffNetPatchFrames);
    }

    [Fact]
    public void Model_reports_expected_embedding_dimension()
    {
        _fixture.Model.EmbeddingDimensions.Should().Be(AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
    }

    [Fact]
    public void Predict_returns_512_dimensional_vectors()
    {
        var embeddings = _fixture.Model.Predict([CreatePatch(FirstPatchValue)]);

        embeddings.Should().HaveCount(SinglePatchCount);
        embeddings[0].Should().HaveCount(AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
    }

    [Fact]
    public void Predict_accepts_less_than_64_patches_and_returns_only_real_count()
    {
        var embeddings = _fixture.Model.Predict([CreatePatch(FirstPatchValue), CreatePatch(SecondPatchValue)]);

        embeddings.Should().HaveCount(TwoPatchCount);
        embeddings.All(embedding => embedding.Length == AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions).Should().BeTrue();
    }

    [Fact]
    public void Predict_accepts_exactly_64_patches()
    {
        var patches = Enumerable.Range(0, FullBatchPatchCount)
            .Select(_ => CreatePatch(FirstPatchValue))
            .ToArray();

        var embeddings = _fixture.Model.Predict(patches);

        embeddings.Should().HaveCount(FullBatchPatchCount);
        embeddings.Should().OnlyContain(embedding => embedding.Length == AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
    }

    [Fact]
    public void Predict_rejects_empty_patch_list()
    {
        var act = () => _fixture.Model.Predict([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Predict_rejects_more_than_batch_size()
    {
        var patches = Enumerable.Range(0, TooManyPatchCount)
            .Select(_ => CreatePatch(FirstPatchValue))
            .ToArray();

        var act = () => _fixture.Model.Predict(patches);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Predict_rejects_wrong_patch_shape()
    {
        var wrongPatch = Enumerable.Range(0, WrongMelBandCount)
            .Select(_ => Enumerable.Repeat(FirstPatchValue, AiModelDefaultValues.DiscogsEffNetPatchFrames).ToArray())
            .ToArray();

        var act = () => _fixture.Model.Predict([wrongPatch]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Predict_reuses_last_patch_to_pad_batch_without_returning_padding_outputs()
    {
        var embeddings = _fixture.Model.Predict([CreatePatch(FirstPatchValue)]);

        embeddings.Should().HaveCount(SinglePatchCount);
    }

    private static float[][] CreatePatch(float value)
    {
        return Enumerable.Range(0, AiModelDefaultValues.DiscogsEffNetMelBands)
            .Select(_ => Enumerable.Repeat(value, AiModelDefaultValues.DiscogsEffNetPatchFrames).ToArray())
            .ToArray();
    }

    public sealed class OnnxModelFixture : IDisposable
    {
        public OnnxModelFixture()
        {
            var pathResolver = new AiModelPathResolver();
            var manifestLoader = new AiModelManifestLoader(pathResolver);
            Model = new OnnxMusicEmbeddingModel(
                manifestLoader,
                pathResolver,
                NullLogger<OnnxMusicEmbeddingModel>.Instance);
        }

        public ILocalMusicEmbeddingModel Model { get; }

        public void Dispose()
        {
            Model.Dispose();
        }
    }
}
