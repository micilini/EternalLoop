using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public sealed class LoopAnalysisDocument
{
    [JsonPropertyName("info")]
    public required LoopAnalysisInfoDocument Info { get; init; }

    [JsonPropertyName("analysis")]
    public required LoopAnalysisBodyDocument Analysis { get; init; }

    [JsonPropertyName("audio_summary")]
    public required LoopAnalysisAudioSummaryDocument AudioSummary { get; init; }
}
