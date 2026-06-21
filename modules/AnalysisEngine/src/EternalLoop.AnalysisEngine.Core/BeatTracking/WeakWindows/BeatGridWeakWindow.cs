using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindow
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("start_time_seconds")]
    public double StartTimeSeconds { get; init; }

    [JsonPropertyName("end_time_seconds")]
    public double EndTimeSeconds { get; init; }

    [JsonPropertyName("legacy_beat_count")]
    public int LegacyBeatCount { get; init; }

    [JsonPropertyName("advisor_beat_count")]
    public int AdvisorBeatCount { get; init; }

    [JsonPropertyName("is_weak_window")]
    public bool IsWeakWindow { get; init; }

    [JsonPropertyName("advisor_is_promising")]
    public bool AdvisorIsPromising { get; init; }

    [JsonPropertyName("future_correction_candidate")]
    public bool FutureCorrectionCandidate { get; init; }

    [JsonPropertyName("risk_level")]
    public BeatGridWeakWindowRiskLevel RiskLevel { get; init; }

    [JsonPropertyName("correction_readiness")]
    public BeatGridWeakWindowCorrectionReadiness CorrectionReadiness { get; init; }

    [JsonPropertyName("advisor_strength")]
    public BeatGridWeakWindowCandidateStrength AdvisorStrength { get; init; }

    [JsonPropertyName("reasons")]
    public IReadOnlyList<BeatGridWeakWindowReason> Reasons { get; init; } = [];

    [JsonPropertyName("metrics")]
    public required BeatGridWeakWindowLocalMetrics Metrics { get; init; }

    [JsonPropertyName("should_apply_correction")]
    public bool ShouldApplyCorrection { get; init; } = false;

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
