using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
using EternalLoop.AnalysisEngine.Core.Models;

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

    [JsonPropertyName("beatProvider")]
    public required AnalysisSummaryBeatProviderDocument BeatProvider { get; init; }
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

public sealed class AnalysisSummaryBeatProviderDocument
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("modelName")]
    public required string ModelName { get; init; }

    [JsonPropertyName("usedAi")]
    public required bool UsedAi { get; init; }

    [JsonPropertyName("usedBuiltIn")]
    public required bool UsedBuiltIn { get; init; }

    [JsonPropertyName("usedFallback")]
    public required bool UsedFallback { get; init; }

    [JsonPropertyName("fallbackReason")]
    public required string? FallbackReason { get; init; }

    [JsonPropertyName("downbeatCount")]
    public required int DownbeatCount { get; init; }

    [JsonPropertyName("beatNumberCount")]
    public required int BeatNumberCount { get; init; }

    [JsonPropertyName("estimatedMeter")]
    public required int? EstimatedMeter { get; init; }

    [JsonPropertyName("beatGridMode")]
    public required string BeatGridMode { get; init; }

    [JsonPropertyName("tatumMode")]
    public required string TatumMode { get; init; }

    [JsonPropertyName("barPhaseMode")]
    public required string BarPhaseMode { get; init; }

    [JsonPropertyName("shadow")]
    public BeatGridShadowDiagnostics? Shadow { get; init; }

    public static AnalysisSummaryBeatProviderDocument From(BeatProviderExportDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new AnalysisSummaryBeatProviderDocument
        {
            Name = diagnostics.Name,
            Mode = diagnostics.Mode,
            ModelName = diagnostics.ModelName,
            UsedAi = diagnostics.UsedAi,
            UsedBuiltIn = diagnostics.UsedBuiltIn,
            UsedFallback = diagnostics.UsedFallback,
            FallbackReason = diagnostics.FallbackReason,
            DownbeatCount = diagnostics.DownbeatCount,
            BeatNumberCount = diagnostics.BeatNumberCount,
            EstimatedMeter = diagnostics.EstimatedMeter,
            BeatGridMode = diagnostics.BeatGridMode,
            TatumMode = diagnostics.TatumMode,
            BarPhaseMode = diagnostics.BarPhaseMode,
            Shadow = diagnostics.Shadow
        };
    }
}
