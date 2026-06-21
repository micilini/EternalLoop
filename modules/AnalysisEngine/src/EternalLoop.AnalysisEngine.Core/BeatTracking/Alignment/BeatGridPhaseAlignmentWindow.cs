using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentWindow
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

    [JsonPropertyName("best_offset_ms")]
    public double BestOffsetMs { get; init; }

    [JsonPropertyName("zero_offset_f1_70ms")]
    public double ZeroOffsetF1_70Ms { get; init; }

    [JsonPropertyName("best_offset_f1_70ms")]
    public double BestOffsetF1_70Ms { get; init; }

    [JsonPropertyName("improvement_f1_70ms")]
    public double ImprovementF1_70Ms { get; init; }

    [JsonPropertyName("is_reliable")]
    public bool IsReliable { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
