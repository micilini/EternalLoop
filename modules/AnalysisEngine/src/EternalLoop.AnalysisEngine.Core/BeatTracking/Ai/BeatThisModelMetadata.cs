using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisModelMetadata
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "beat-this";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "unknown";

    [JsonPropertyName("license")]
    public string License { get; init; } = "unknown";

    [JsonPropertyName("model_file")]
    public string ModelFile { get; init; } = BeatThisModelLocator.DefaultModelFileName;

    [JsonPropertyName("model_sha256")]
    public string? ModelSha256 { get; init; }

    [JsonPropertyName("sample_rate")]
    public int SampleRate { get; init; } = 22_050;

    [JsonPropertyName("frame_rate")]
    public double FrameRate { get; init; } = 50.0;

    [JsonPropertyName("input_name")]
    public string InputName { get; init; } = "spectrogram";

    [JsonPropertyName("output_names")]
    public string[] OutputNames { get; init; } = [];

    [JsonPropertyName("onnx_kind")]
    public string OnnxKind { get; init; } = "spectrogram-to-frame-logits";

    [JsonPropertyName("chunk_frames")]
    public int ChunkFrames { get; init; } = 1_500;

    [JsonPropertyName("mel_bins")]
    public int MelBins { get; init; } = 128;

    [JsonPropertyName("frame_size")]
    public int FrameSize { get; init; } = 1_024;
}
