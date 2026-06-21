using System.Security.Cryptography;
using System.Text;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class BeatThisModelLocatorTests
{
    [Fact]
    public void GetAvailability_returns_unavailable_when_directory_is_missing()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var locator = new BeatThisModelLocator(directory);

        var availability = locator.GetAvailability();

        availability.IsAvailable.Should().BeFalse();
        availability.ErrorMessage.Should().Contain("model directory was not found");
    }

    [Fact]
    public void GetAvailability_returns_unavailable_when_model_file_is_missing()
    {
        using var temp = TemporaryDirectory.Create();
        var locator = new BeatThisModelLocator(temp.Path);

        var availability = locator.GetAvailability();

        availability.IsAvailable.Should().BeFalse();
        availability.ErrorMessage.Should().Contain("ONNX model file was not found");
    }

    [Fact]
    public void GetAvailability_returns_available_when_model_exists_without_metadata()
    {
        using var temp = TemporaryDirectory.Create();
        var modelPath = Path.Combine(temp.Path, BeatThisModelLocator.DefaultModelFileName);
        File.WriteAllBytes(modelPath, [1, 2, 3, 4]);

        var locator = new BeatThisModelLocator(temp.Path);

        var availability = locator.GetAvailability();

        availability.IsAvailable.Should().BeTrue();
        availability.ModelPath.Should().Be(Path.GetFullPath(modelPath));
        availability.MetadataPath.Should().BeNull();
        availability.ModelSha256.Should().Be(ComputeSha256([1, 2, 3, 4]));
        availability.Metadata.Should().NotBeNull();
        availability.Metadata!.Name.Should().Be("beat-this");
    }

    [Fact]
    public void GetAvailability_reads_metadata_and_uses_declared_model_file()
    {
        using var temp = TemporaryDirectory.Create();
        var modelBytes = new byte[] { 9, 8, 7, 6 };
        var modelHash = ComputeSha256(modelBytes);
        var modelPath = Path.Combine(temp.Path, "custom-beat-this.onnx");
        File.WriteAllBytes(modelPath, modelBytes);
        File.WriteAllText(
            Path.Combine(temp.Path, BeatThisModelLocator.DefaultMetadataFileName),
            $$"""
            {
              "name": "beat-this-large",
              "version": "test",
              "license": "MIT",
              "model_file": "custom-beat-this.onnx",
              "model_sha256": "{{modelHash}}",
              "sample_rate": 22050,
              "frame_rate": 50.0,
              "input_name": "audio",
              "output_names": ["beats", "downbeats"]
            }
            """);

        var locator = new BeatThisModelLocator(temp.Path);

        var availability = locator.GetAvailability();

        availability.IsAvailable.Should().BeTrue();
        availability.ModelPath.Should().Be(Path.GetFullPath(modelPath));
        availability.MetadataPath.Should().Be(Path.GetFullPath(Path.Combine(temp.Path, BeatThisModelLocator.DefaultMetadataFileName)));
        availability.ModelSha256.Should().Be(modelHash);
        availability.Metadata!.Name.Should().Be("beat-this-large");
        availability.Metadata.ModelFile.Should().Be("custom-beat-this.onnx");
        availability.Metadata.License.Should().Be("MIT");
        availability.Metadata.InputName.Should().Be("audio");
        availability.Metadata.OutputNames.Should().Equal("beats", "downbeats");
    }

    [Fact]
    public void GetAvailability_returns_unavailable_when_metadata_hash_does_not_match()
    {
        using var temp = TemporaryDirectory.Create();
        var modelPath = Path.Combine(temp.Path, BeatThisModelLocator.DefaultModelFileName);
        File.WriteAllBytes(modelPath, [1, 2, 3, 4]);
        File.WriteAllText(
            Path.Combine(temp.Path, BeatThisModelLocator.DefaultMetadataFileName),
            """
            {
              "model_sha256": "0000000000000000000000000000000000000000000000000000000000000000"
            }
            """);

        var locator = new BeatThisModelLocator(temp.Path);

        var availability = locator.GetAvailability();

        availability.IsAvailable.Should().BeFalse();
        availability.ErrorMessage.Should().Contain("hash mismatch");
    }

    [Fact]
    public void GetAvailability_throws_clear_error_when_metadata_json_is_invalid()
    {
        using var temp = TemporaryDirectory.Create();
        File.WriteAllBytes(Path.Combine(temp.Path, BeatThisModelLocator.DefaultModelFileName), [1, 2, 3, 4]);
        File.WriteAllText(Path.Combine(temp.Path, BeatThisModelLocator.DefaultMetadataFileName), "{ invalid json");

        var locator = new BeatThisModelLocator(temp.Path);

        var act = () => locator.GetAvailability();

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Beat This model metadata is invalid JSON:*");
    }

    private static string ComputeSha256(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            return new TemporaryDirectory(System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "eternalloop-beat-this-tests",
                Guid.NewGuid().ToString("N")));
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
