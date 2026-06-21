using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionWindow
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("source_weak_window_index")]
    public int SourceWeakWindowIndex { get; init; }

    [JsonPropertyName("start_time_seconds")]
    public double StartTimeSeconds { get; init; }

    [JsonPropertyName("end_time_seconds")]
    public double EndTimeSeconds { get; init; }

    [JsonPropertyName("decision")]
    public BeatGridWeakWindowCorrectionDecision Decision { get; init; }

    [JsonPropertyName("risk")]
    public BeatGridWeakWindowCorrectionRisk Risk { get; init; }

    [JsonPropertyName("readiness")]
    public BeatGridWeakWindowCorrectionReadiness Readiness { get; init; }

    [JsonPropertyName("legacy_beat_count_before")]
    public int LegacyBeatCountBefore { get; init; }

    [JsonPropertyName("advisor_beat_count_used")]
    public int AdvisorBeatCountUsed { get; init; }

    [JsonPropertyName("corrected_beat_count_after")]
    public int CorrectedBeatCountAfter { get; init; }

    [JsonPropertyName("replacement_count_delta")]
    public int ReplacementCountDelta { get; init; }

    [JsonPropertyName("applied_offset_ms")]
    public double? AppliedOffsetMs { get; init; }

    [JsonPropertyName("boundary_adjustment_applied")]
    public bool BoundaryAdjustmentApplied { get; init; }

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
