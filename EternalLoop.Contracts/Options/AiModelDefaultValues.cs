namespace EternalLoop.Contracts.Options;

public static class AiModelDefaultValues
{
    public const string DiscogsEffNetModelId = "discogs-track-effnet-bs64-v1";
    public const string DiscogsEffNetDisplayName = "Discogs Track EffNet Embeddings";
    public const string DiscogsEffNetProvider = "MTG / Essentia";
    public const string DiscogsEffNetVersion = "1";
    public const string DiscogsEffNetOnnxFile = "discogs_track_embeddings-effnet-bs64-1.onnx";
    public const string DiscogsEffNetMetadataFile = "discogs_track_embeddings-effnet-bs64-1.json";
    public const string DiscogsEffNetLicenseNoticeFile = "MODEL-LICENSE-NOTICE.txt";
    public const string DiscogsEffNetInputName = "serving_default_melspectrogram";
    public const string DiscogsEffNetEmbeddingOutputName = "PartitionedCall:1";
    public const string DiscogsEffNetLicense = "CC BY-NC-SA 4.0";
    public const string DiscogsEffNetSource = "https://essentia.upf.edu/models/feature-extractors/discogs-effnet/";
    public const int DiscogsEffNetBatchSize = 64;
    public const int DiscogsEffNetMelBands = 128;
    public const int DiscogsEffNetPatchFrames = 96;
    public const int DiscogsEffNetEmbeddingDimensions = 1280;
    public const int DiscogsEffNetSampleRate = 16000;
}
