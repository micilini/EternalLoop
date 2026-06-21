using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public sealed class LoopAnalysisBeatProviderDocument
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("license")]
    public required string License { get; init; }

    [JsonPropertyName("model_name")]
    public required string ModelName { get; init; }

    [JsonPropertyName("model_sha256")]
    public required string ModelSha256 { get; init; }

    [JsonPropertyName("used_ai")]
    public required bool UsedAi { get; init; }

    [JsonPropertyName("used_built_in")]
    public required bool UsedBuiltIn { get; init; }

    [JsonPropertyName("used_fallback")]
    public required bool UsedFallback { get; init; }

    [JsonPropertyName("fallback_reason")]
    public required string? FallbackReason { get; init; }

    [JsonPropertyName("downbeat_count")]
    public required int DownbeatCount { get; init; }

    [JsonPropertyName("beat_number_count")]
    public required int BeatNumberCount { get; init; }

    [JsonPropertyName("estimated_meter")]
    public required int? EstimatedMeter { get; init; }

    [JsonPropertyName("beat_grid_mode")]
    public required string BeatGridMode { get; init; }

    [JsonPropertyName("tatum_mode")]
    public required string TatumMode { get; init; }

    [JsonPropertyName("requested_tatum_mode")]
    public required string RequestedTatumMode { get; init; }

    [JsonPropertyName("bar_phase_mode")]
    public required string BarPhaseMode { get; init; }

    [JsonPropertyName("shadow")]
    public BeatGridShadowDiagnostics? Shadow { get; init; }

    public static LoopAnalysisBeatProviderDocument From(BeatProviderExportDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new LoopAnalysisBeatProviderDocument
        {
            Name = diagnostics.Name,
            Mode = diagnostics.Mode,
            Version = diagnostics.Version,
            License = diagnostics.License,
            ModelName = diagnostics.ModelName,
            ModelSha256 = diagnostics.ModelSha256,
            UsedAi = diagnostics.UsedAi,
            UsedBuiltIn = diagnostics.UsedBuiltIn,
            UsedFallback = diagnostics.UsedFallback,
            FallbackReason = diagnostics.FallbackReason,
            DownbeatCount = diagnostics.DownbeatCount,
            BeatNumberCount = diagnostics.BeatNumberCount,
            EstimatedMeter = diagnostics.EstimatedMeter,
            BeatGridMode = diagnostics.BeatGridMode,
            TatumMode = diagnostics.TatumMode,
            RequestedTatumMode = diagnostics.RequestedTatumMode,
            BarPhaseMode = diagnostics.BarPhaseMode,
            Shadow = diagnostics.Shadow
        };
    }
}
