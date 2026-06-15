using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public sealed class LoopAnalysisTimeQuantumDocument
{
    [JsonPropertyName("start")]
    public required double Start { get; init; }

    [JsonPropertyName("duration")]
    public required double Duration { get; init; }

    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }
}
