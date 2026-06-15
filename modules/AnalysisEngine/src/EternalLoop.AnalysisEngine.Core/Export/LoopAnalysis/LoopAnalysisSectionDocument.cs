using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public sealed class LoopAnalysisSectionDocument
{
    [JsonPropertyName("start")]
    public required double Start { get; init; }

    [JsonPropertyName("duration")]
    public required double Duration { get; init; }

    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }

    [JsonPropertyName("loudness")]
    public required double Loudness { get; init; }

    [JsonPropertyName("tempo")]
    public required double Tempo { get; init; }

    [JsonPropertyName("tempo_confidence")]
    public required double TempoConfidence { get; init; }

    [JsonPropertyName("key")]
    public required int Key { get; init; }

    [JsonPropertyName("key_confidence")]
    public required double KeyConfidence { get; init; }

    [JsonPropertyName("mode")]
    public required int Mode { get; init; }

    [JsonPropertyName("mode_confidence")]
    public required double ModeConfidence { get; init; }

    [JsonPropertyName("time_signature")]
    public required int TimeSignature { get; init; }

    [JsonPropertyName("time_signature_confidence")]
    public required double TimeSignatureConfidence { get; init; }
}
