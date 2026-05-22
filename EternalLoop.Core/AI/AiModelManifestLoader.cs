using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using System.Text.Json;

namespace EternalLoop.Core.AI;

public sealed class AiModelManifestLoader : IAiModelProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AiModelPathResolver _pathResolver;

    public AiModelManifestLoader(AiModelPathResolver pathResolver)
    {
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
    }

    public AiModelManifest Load()
    {
        var manifestPath = _pathResolver.ResolveManifestPath();
        var manifest = JsonSerializer.Deserialize<AiModelManifest>(File.ReadAllText(manifestPath), JsonOptions);

        if (manifest is null)
        {
            throw new InvalidDataException($"AI model manifest could not be deserialized from '{manifestPath}'.");
        }

        ValidateManifest(manifest);
        _pathResolver.ResolveOnnxPath(manifest);
        _pathResolver.ResolveMetadataPath(manifest);
        _pathResolver.ResolveLicenseNoticePath(manifest);
        return manifest;
    }

    public Task<AiModelManifest> GetManifestAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Load());
    }

    private static void ValidateManifest(AiModelManifest manifest)
    {
        RequireText(manifest.Id, nameof(manifest.Id));
        RequireText(manifest.Version, nameof(manifest.Version));
        RequireText(manifest.OnnxFile, nameof(manifest.OnnxFile));
        RequireText(manifest.MetadataFile, nameof(manifest.MetadataFile));
        RequireText(manifest.LicenseNoticeFile, nameof(manifest.LicenseNoticeFile));
        RequireText(manifest.InputName, nameof(manifest.InputName));
        RequireText(manifest.EmbeddingOutputName, nameof(manifest.EmbeddingOutputName));

        RequireEqual(manifest.BatchSize, AiModelDefaultValues.DiscogsEffNetBatchSize, nameof(manifest.BatchSize));
        RequireEqual(manifest.MelBands, AiModelDefaultValues.DiscogsEffNetMelBands, nameof(manifest.MelBands));
        RequireEqual(manifest.PatchFrames, AiModelDefaultValues.DiscogsEffNetPatchFrames, nameof(manifest.PatchFrames));
        RequireEqual(manifest.EmbeddingDimensions, AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions, nameof(manifest.EmbeddingDimensions));
        RequireEqual(manifest.SampleRate, AiModelDefaultValues.DiscogsEffNetSampleRate, nameof(manifest.SampleRate));
        RequireEqual(manifest.InputName, AiModelDefaultValues.DiscogsEffNetInputName, nameof(manifest.InputName));
        RequireEqual(manifest.EmbeddingOutputName, AiModelDefaultValues.DiscogsEffNetEmbeddingOutputName, nameof(manifest.EmbeddingOutputName));
    }

    private static void RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"AI model manifest field '{fieldName}' is required.");
        }
    }

    private static void RequireEqual(int value, int expected, string fieldName)
    {
        if (value != expected)
        {
            throw new InvalidDataException($"AI model manifest field '{fieldName}' must be '{expected}' but was '{value}'.");
        }
    }

    private static void RequireEqual(string value, string expected, string fieldName)
    {
        if (!string.Equals(value, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"AI model manifest field '{fieldName}' must be '{expected}' but was '{value}'.");
        }
    }
}
