using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;

namespace EternalLoop.AnalysisEngine.Core.Models;

public sealed class BeatProviderExportDiagnostics
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "built-in";

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "dsp";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "analysisengine-built-in";

    [JsonPropertyName("license")]
    public string License { get; init; } = "MIT";

    [JsonPropertyName("model_name")]
    public string ModelName { get; init; } = "none";

    [JsonPropertyName("model_sha256")]
    public string ModelSha256 { get; init; } = "none";

    [JsonPropertyName("used_ai")]
    public bool UsedAi { get; init; }

    [JsonPropertyName("used_built_in")]
    public bool UsedBuiltIn { get; init; } = true;

    [JsonPropertyName("used_fallback")]
    public bool UsedFallback { get; init; }

    [JsonPropertyName("used_hybrid")]
    public bool UsedHybrid { get; init; }

    [JsonPropertyName("fallback_reason")]
    public string? FallbackReason { get; init; }

    [JsonPropertyName("provider_warnings")]
    public IReadOnlyList<string> ProviderWarnings { get; init; } = [];

    [JsonPropertyName("downbeat_sanitized")]
    public bool DownbeatSanitized { get; init; }

    [JsonPropertyName("downbeat_count")]
    public int DownbeatCount { get; init; }

    [JsonPropertyName("beat_number_count")]
    public int BeatNumberCount { get; init; }

    [JsonPropertyName("estimated_meter")]
    public int? EstimatedMeter { get; init; }

    [JsonPropertyName("beat_grid_mode")]
    public string BeatGridMode { get; init; } = "unknown";

    [JsonPropertyName("tatum_mode")]
    public string TatumMode { get; init; } = "uniform-fallback";

    [JsonPropertyName("requested_tatum_mode")]
    public string RequestedTatumMode { get; init; } = "Default";

    [JsonPropertyName("bar_phase_mode")]
    public string BarPhaseMode { get; init; } = "phase-zero";

    [JsonPropertyName("shadow")]
    public BeatGridShadowDiagnostics? Shadow { get; init; }

    [JsonPropertyName("candidates")]
    public BeatGridCandidateSet? Candidates { get; init; }

    public static BeatProviderExportDiagnostics BuiltIn()
    {
        return new BeatProviderExportDiagnostics();
    }

    public static BeatProviderExportDiagnostics FromDiagnostics(AnalysisDiagnostics? diagnostics)
    {
        if (diagnostics is null)
        {
            return BuiltIn();
        }

        return new BeatProviderExportDiagnostics
        {
            Name = diagnostics.BeatProviderName,
            Mode = ResolveMode(diagnostics),
            Version = diagnostics.BeatProviderVersion,
            License = diagnostics.BeatProviderLicense,
            ModelName = diagnostics.BeatProviderModelName,
            ModelSha256 = diagnostics.BeatProviderModelSha256,
            UsedAi = diagnostics.BeatProviderUsedAi,
            UsedBuiltIn = diagnostics.BeatProviderUsedBuiltIn,
            UsedFallback = diagnostics.BeatProviderUsedFallback,
            UsedHybrid = diagnostics.BeatProviderUsedHybrid,
            FallbackReason = diagnostics.BeatProviderFallbackReason,
            ProviderWarnings = diagnostics.BeatProviderWarnings,
            DownbeatSanitized = diagnostics.BeatProviderDownbeatSanitized,
            DownbeatCount = diagnostics.BeatProviderDownbeatCount,
            BeatNumberCount = diagnostics.BeatProviderBeatNumberCount,
            EstimatedMeter = diagnostics.BeatProviderEstimatedMeter,
            BeatGridMode = diagnostics.BeatGridMode,
            TatumMode = diagnostics.TatumMode,
            RequestedTatumMode = diagnostics.RequestedTatumMode,
            BarPhaseMode = diagnostics.BarPhaseMode,
            Shadow = diagnostics.BeatProviderShadowDiagnostics,
            Candidates = diagnostics.BeatProviderCandidateSet
        };
    }

    private static string ResolveMode(AnalysisDiagnostics diagnostics)
    {
        if (diagnostics.BeatProviderUsedHybrid && diagnostics.BeatProviderUsedFallback)
        {
            return "hybrid-fallback";
        }

        if (diagnostics.BeatProviderUsedHybrid)
        {
            return "hybrid-experimental";
        }

        if (diagnostics.BeatProviderUsedFallback)
        {
            return "fallback";
        }

        if (diagnostics.BeatProviderUsedAi)
        {
            return "onnx-local";
        }

        if (diagnostics.BeatProviderShadowDiagnostics?.Enabled == true)
        {
            return "dsp-shadow";
        }

        return "dsp";
    }
}
