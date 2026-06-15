using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export.Summary;

public sealed class AnalysisSummaryDocument
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("input")]
    public required string Input { get; init; }

    [JsonPropertyName("fileHash")]
    public required string FileHash { get; init; }

    [JsonPropertyName("durationSeconds")]
    public required double DurationSeconds { get; init; }

    [JsonPropertyName("sampleRate")]
    public required int SampleRate { get; init; }

    [JsonPropertyName("tempo")]
    public required double Tempo { get; init; }

    [JsonPropertyName("timeSignature")]
    public required int TimeSignature { get; init; }

    [JsonPropertyName("counts")]
    public required AnalysisSummaryCountsDocument Counts { get; init; }

    [JsonPropertyName("outputs")]
    public required AnalysisSummaryOutputsDocument Outputs { get; init; }
}

public sealed class AnalysisSummaryCountsDocument
{
    [JsonPropertyName("segments")]
    public required int Segments { get; init; }

    [JsonPropertyName("beats")]
    public required int Beats { get; init; }

    [JsonPropertyName("bars")]
    public required int Bars { get; init; }

    [JsonPropertyName("tatums")]
    public required int Tatums { get; init; }

    [JsonPropertyName("sections")]
    public required int Sections { get; init; }
}

public sealed class AnalysisSummaryOutputsDocument
{
    [JsonPropertyName("raw")]
    public required string? Raw { get; init; }

    [JsonPropertyName("loopAnalysis")]
    public required string? LoopAnalysis { get; init; }
}
