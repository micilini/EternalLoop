using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using FluentAssertions;
using System.Text.Json;

namespace EternalLoop.Core.Tests.Contracts;

public sealed class AiContractsTests
{
    private const int ExpectedBeatIndex = 3;
    private const int ExpectedSampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate;
    private const int ExpectedEmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions;
    private const int ExpectedBatchSize = AiModelDefaultValues.DiscogsEffNetBatchSize;
    private const int ExpectedMelBands = AiModelDefaultValues.DiscogsEffNetMelBands;
    private const int ExpectedPatchFrames = AiModelDefaultValues.DiscogsEffNetPatchFrames;

    [Fact]
    public void AiAnalysisData_Should_SerializeAndDeserialize()
    {
        var data = new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = ExpectedSampleRate,
            EmbeddingDimensions = ExpectedEmbeddingDimensions,
            BeatEmbeddings =
            [
                new AiBeatEmbedding
                {
                    BeatIndex = ExpectedBeatIndex,
                    Vector = [0.1f, 0.2f, 0.3f]
                }
            ]
        };

        var json = JsonSerializer.Serialize(data);
        var restored = JsonSerializer.Deserialize<AiAnalysisData>(json);

        restored.Should().NotBeNull();
        restored!.ModelId.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
        restored.ModelVersion.Should().Be(AiModelDefaultValues.DiscogsEffNetVersion);
        restored.SampleRate.Should().Be(ExpectedSampleRate);
        restored.EmbeddingDimensions.Should().Be(ExpectedEmbeddingDimensions);
        restored.BeatEmbeddings.Should().HaveCount(1);
        restored.BeatEmbeddings[0].BeatIndex.Should().Be(ExpectedBeatIndex);
        restored.BeatEmbeddings[0].Vector.Should().Equal(0.1f, 0.2f, 0.3f);
    }

    [Fact]
    public void AiAnalysisOptions_Should_ExposeExpectedDefaults()
    {
        var options = new AiAnalysisOptions();

        options.IsEnabled.Should().BeTrue();
        options.ModelId.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
        options.RejectionThreshold.Should().Be(TuningDefaultValues.AiRejectionThreshold);
        options.PenaltyStartThreshold.Should().Be(TuningDefaultValues.AiPenaltyStartThreshold);
        options.PenaltyStrength.Should().Be(TuningDefaultValues.AiPenaltyStrength);
        options.BeatContextBefore.Should().Be(TuningDefaultValues.AiBeatContextBefore);
        options.BeatContextAfter.Should().Be(TuningDefaultValues.AiBeatContextAfter);
    }

    [Fact]
    public void AiModelManifest_Should_DeserializeManifestShape()
    {
        var json = $$"""
            {
              "id": "{{AiModelDefaultValues.DiscogsEffNetModelId}}",
              "displayName": "{{AiModelDefaultValues.DiscogsEffNetDisplayName}}",
              "provider": "{{AiModelDefaultValues.DiscogsEffNetProvider}}",
              "version": "{{AiModelDefaultValues.DiscogsEffNetVersion}}",
              "onnxFile": "{{AiModelDefaultValues.DiscogsEffNetOnnxFile}}",
              "metadataFile": "{{AiModelDefaultValues.DiscogsEffNetMetadataFile}}",
              "licenseNoticeFile": "{{AiModelDefaultValues.DiscogsEffNetLicenseNoticeFile}}",
              "inputName": "{{AiModelDefaultValues.DiscogsEffNetInputName}}",
              "embeddingOutputName": "{{AiModelDefaultValues.DiscogsEffNetEmbeddingOutputName}}",
              "batchSize": {{ExpectedBatchSize}},
              "melBands": {{ExpectedMelBands}},
              "patchFrames": {{ExpectedPatchFrames}},
              "embeddingDimensions": {{ExpectedEmbeddingDimensions}},
              "sampleRate": {{ExpectedSampleRate}},
              "license": "{{AiModelDefaultValues.DiscogsEffNetLicense}}",
              "source": "{{AiModelDefaultValues.DiscogsEffNetSource}}"
            }
            """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var manifest = JsonSerializer.Deserialize<AiModelManifest>(json, options);

        manifest.Should().NotBeNull();
        manifest!.Id.Should().Be(AiModelDefaultValues.DiscogsEffNetModelId);
        manifest.BatchSize.Should().Be(ExpectedBatchSize);
        manifest.MelBands.Should().Be(ExpectedMelBands);
        manifest.PatchFrames.Should().Be(ExpectedPatchFrames);
        manifest.EmbeddingDimensions.Should().Be(ExpectedEmbeddingDimensions);
        manifest.SampleRate.Should().Be(ExpectedSampleRate);
    }
}
