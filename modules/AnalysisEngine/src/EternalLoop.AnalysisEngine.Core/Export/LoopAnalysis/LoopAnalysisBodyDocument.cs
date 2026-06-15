using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public sealed class LoopAnalysisBodyDocument
{
    [JsonPropertyName("sections")]
    public required IReadOnlyList<LoopAnalysisSectionDocument> Sections { get; init; }

    [JsonPropertyName("bars")]
    public required IReadOnlyList<LoopAnalysisTimeQuantumDocument> Bars { get; init; }

    [JsonPropertyName("beats")]
    public required IReadOnlyList<LoopAnalysisTimeQuantumDocument> Beats { get; init; }

    [JsonPropertyName("tatums")]
    public required IReadOnlyList<LoopAnalysisTimeQuantumDocument> Tatums { get; init; }

    [JsonPropertyName("segments")]
    public required IReadOnlyList<LoopAnalysisSegmentDocument> Segments { get; init; }
}
