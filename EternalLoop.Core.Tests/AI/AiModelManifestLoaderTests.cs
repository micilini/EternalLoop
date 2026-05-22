using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiModelManifestLoaderTests
{
    [Fact]
    public void Model_manifest_loads_successfully()
    {
        var loader = CreateLoader();

        var manifest = loader.Load();

        manifest.Should().NotBeNull();
    }

    [Fact]
    public void Model_manifest_reports_expected_model_id()
    {
        var manifest = CreateLoader().Load();

        manifest.Id.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
    }

    [Fact]
    public void Model_manifest_reports_expected_input_and_output_names()
    {
        var manifest = CreateLoader().Load();

        manifest.InputName.Should().Be(AiModelDefaultValues.DiscogsEffNetInputName);
        manifest.EmbeddingOutputName.Should().Be(AiModelDefaultValues.DiscogsEffNetEmbeddingOutputName);
    }

    [Fact]
    public void Model_manifest_reports_expected_shape_values()
    {
        var manifest = CreateLoader().Load();

        manifest.BatchSize.Should().Be(AiModelDefaultValues.DiscogsEffNetBatchSize);
        manifest.MelBands.Should().Be(AiModelDefaultValues.DiscogsEffNetMelBands);
        manifest.PatchFrames.Should().Be(AiModelDefaultValues.DiscogsEffNetPatchFrames);
        manifest.EmbeddingDimensions.Should().Be(AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions);
        manifest.SampleRate.Should().Be(AiModelDefaultValues.DiscogsEffNetSampleRate);
    }

    [Fact]
    public void Model_paths_resolve_from_test_context()
    {
        var resolver = new AiModelPathResolver();

        var modelDirectory = resolver.ResolveModelDirectory();

        Directory.Exists(modelDirectory).Should().BeTrue();
        modelDirectory.Should().EndWith(Path.Combine("Assets", "Models", "DiscogsEffNet"));
    }

    [Fact]
    public void Model_paths_resolve_existing_manifest_and_notice()
    {
        var resolver = new AiModelPathResolver();
        var manifest = new AiModelManifestLoader(resolver).Load();

        File.Exists(resolver.ResolveManifestPath()).Should().BeTrue();
        File.Exists(resolver.ResolveLicenseNoticePath(manifest)).Should().BeTrue();
    }

    private static AiModelManifestLoader CreateLoader()
    {
        return new AiModelManifestLoader(new AiModelPathResolver());
    }
}
