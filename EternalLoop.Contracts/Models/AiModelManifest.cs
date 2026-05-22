namespace EternalLoop.Contracts.Models;

public sealed class AiModelManifest
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Provider { get; init; }

    public required string Version { get; init; }

    public required string OnnxFile { get; init; }

    public required string MetadataFile { get; init; }

    public required string LicenseNoticeFile { get; init; }

    public required string InputName { get; init; }

    public required string EmbeddingOutputName { get; init; }

    public required int BatchSize { get; init; }

    public required int MelBands { get; init; }

    public required int PatchFrames { get; init; }

    public required int EmbeddingDimensions { get; init; }

    public required int SampleRate { get; init; }

    public required string License { get; init; }

    public required string Source { get; init; }

    public string? OnnxUrl { get; init; }

    public string? MetadataUrl { get; init; }
}
