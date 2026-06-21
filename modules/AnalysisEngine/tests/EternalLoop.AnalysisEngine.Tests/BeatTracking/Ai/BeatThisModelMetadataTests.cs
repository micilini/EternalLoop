using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class BeatThisModelMetadataTests
{
    [Fact]
    public void Defaults_match_spectrogram_to_logits_contract()
    {
        var metadata = new BeatThisModelMetadata();

        metadata.InputName.Should().Be("spectrogram");
        metadata.OnnxKind.Should().Be("spectrogram-to-frame-logits");
        metadata.SampleRate.Should().Be(22_050);
        metadata.FrameRate.Should().Be(50.0);
        metadata.ChunkFrames.Should().Be(1_500);
        metadata.MelBins.Should().Be(128);
        metadata.FrameSize.Should().Be(1_024);
    }

    [Fact]
    public void Deserializes_phase_07_and_08_contract_fields()
    {
        const string json = """
        {
          "name": "beat-this-large",
          "version": "final0",
          "license": "MIT",
          "model_file": "beat-this-large.onnx",
          "model_sha256": "abc123",
          "sample_rate": 22050,
          "frame_rate": 50.0,
          "input_name": "spectrogram",
          "output_names": ["beat_logits", "downbeat_logits"],
          "onnx_kind": "spectrogram-to-frame-logits",
          "chunk_frames": 1500,
          "mel_bins": 128,
          "frame_size": 1024
        }
        """;

        var metadata = JsonSerializer.Deserialize<BeatThisModelMetadata>(json);

        metadata.Should().NotBeNull();
        metadata!.Name.Should().Be("beat-this-large");
        metadata.OutputNames.Should().Equal("beat_logits", "downbeat_logits");
        metadata.OnnxKind.Should().Be("spectrogram-to-frame-logits");
        metadata.ChunkFrames.Should().Be(1_500);
        metadata.MelBins.Should().Be(128);
        metadata.FrameSize.Should().Be(1_024);
    }

    [Fact]
    public void BeatThisModelMetadata_default_frame_rate_matches_python_advisor_contract()
    {
        var metadata = new BeatThisModelMetadata();

        metadata.FrameRate.Should().Be(50.0);
    }

    [Fact]
    public void Model_json_matches_python_advisor_contract()
    {
        var json = File.ReadAllText(FindRepoFile(Path.Combine("assets", "models", "beat-this", "model.json")));
        var metadata = JsonSerializer.Deserialize<BeatThisModelMetadata>(json);

        metadata.Should().NotBeNull();
        metadata!.FrameRate.Should().Be(50.0);
        metadata.ChunkFrames.Should().Be(1_500);
        metadata.MelBins.Should().Be(128);
        metadata.OutputNames.Should().Equal("beat_logits", "downbeat_logits");
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file: {relativePath}");
    }
}
