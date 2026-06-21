namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisAvailability
{
    private BeatThisAvailability(
        bool isAvailable,
        string? modelPath,
        string? metadataPath,
        string? modelSha256,
        BeatThisModelMetadata? metadata,
        string? errorMessage)
    {
        IsAvailable = isAvailable;
        ModelPath = modelPath;
        MetadataPath = metadataPath;
        ModelSha256 = modelSha256;
        Metadata = metadata;
        ErrorMessage = errorMessage;
    }

    public bool IsAvailable { get; }

    public string? ModelPath { get; }

    public string? MetadataPath { get; }

    public string? ModelSha256 { get; }

    public BeatThisModelMetadata? Metadata { get; }

    public string? ErrorMessage { get; }

    public static BeatThisAvailability Available(
        string modelPath,
        string? metadataPath,
        string modelSha256,
        BeatThisModelMetadata metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelSha256);
        ArgumentNullException.ThrowIfNull(metadata);

        return new BeatThisAvailability(
            true,
            modelPath,
            metadataPath,
            modelSha256,
            metadata,
            null);
    }

    public static BeatThisAvailability Unavailable(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new BeatThisAvailability(
            false,
            null,
            null,
            null,
            null,
            errorMessage);
    }
}