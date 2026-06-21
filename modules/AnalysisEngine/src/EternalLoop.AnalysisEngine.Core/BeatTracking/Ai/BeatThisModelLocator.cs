using System.Security.Cryptography;
using System.Text.Json;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisModelLocator
{
    public const string DefaultModelDirectory = "assets/models/beat-this";

    public const string DefaultModelFileName = "beat-this-large.onnx";

    public const string DefaultMetadataFileName = "model.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string? _modelDirectory;

    public BeatThisModelLocator(string? modelDirectory = null)
    {
        _modelDirectory = modelDirectory;
    }

    public BeatThisAvailability GetAvailability()
    {
        var directory = ResolveModelDirectory();

        if (string.IsNullOrWhiteSpace(directory))
        {
            return BeatThisAvailability.Unavailable("Beat This model directory was not configured.");
        }

        if (!Directory.Exists(directory))
        {
            return BeatThisAvailability.Unavailable($"Beat This model directory was not found: {directory}");
        }

        var metadataPath = Path.Combine(directory, DefaultMetadataFileName);
        var metadata = LoadMetadata(metadataPath);
        var modelFileName = string.IsNullOrWhiteSpace(metadata.ModelFile)
            ? DefaultModelFileName
            : metadata.ModelFile;
        var modelPath = Path.Combine(directory, modelFileName);

        if (!File.Exists(modelPath))
        {
            return BeatThisAvailability.Unavailable($"Beat This ONNX model file was not found: {modelPath}");
        }

        var modelSha256 = ComputeSha256(modelPath);

        if (!string.IsNullOrWhiteSpace(metadata.ModelSha256)
            && !string.Equals(metadata.ModelSha256, modelSha256, StringComparison.OrdinalIgnoreCase))
        {
            return BeatThisAvailability.Unavailable(
                $"Beat This ONNX model hash mismatch. Expected {metadata.ModelSha256}, got {modelSha256}.");
        }

        return BeatThisAvailability.Available(
            Path.GetFullPath(modelPath),
            File.Exists(metadataPath) ? Path.GetFullPath(metadataPath) : null,
            modelSha256,
            metadata);
    }

    private string ResolveModelDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_modelDirectory))
        {
            return Path.GetFullPath(_modelDirectory);
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, DefaultModelDirectory));
    }

    private static BeatThisModelMetadata LoadMetadata(string metadataPath)
    {
        if (!File.Exists(metadataPath))
        {
            return new BeatThisModelMetadata();
        }

        try
        {
            var json = File.ReadAllText(metadataPath);
            var metadata = JsonSerializer.Deserialize<BeatThisModelMetadata>(json, JsonOptions);

            return metadata ?? new BeatThisModelMetadata();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Beat This model metadata is invalid JSON: {metadataPath}", ex);
        }
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}