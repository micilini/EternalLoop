using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public sealed class LoopAnalysisAudioSummaryDocument
{
    [JsonPropertyName("duration")]
    public required double Duration { get; init; }
}
