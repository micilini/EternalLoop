using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

public sealed class BeatGridCandidateQuality
{
    [JsonPropertyName("beat_count")]
    public int BeatCount { get; init; }

    [JsonPropertyName("downbeat_count")]
    public int DownbeatCount { get; init; }

    [JsonPropertyName("estimated_bpm")]
    public double? EstimatedBpm { get; init; }

    [JsonPropertyName("median_interval_seconds")]
    public double? MedianIntervalSeconds { get; init; }

    [JsonPropertyName("beat_density_per_second")]
    public double? BeatDensityPerSecond { get; init; }

    [JsonPropertyName("is_dense_grid")]
    public bool IsDenseGrid { get; init; }

    [JsonPropertyName("is_plausible")]
    public bool IsPlausible { get; init; }

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
