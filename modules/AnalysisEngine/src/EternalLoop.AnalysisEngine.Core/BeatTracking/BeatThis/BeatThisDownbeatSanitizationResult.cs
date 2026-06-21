using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.BeatThis;

public sealed class BeatThisDownbeatSanitizationResult
{
    [JsonPropertyName("input_downbeat_count")]
    public int InputDownbeatCount { get; init; }

    [JsonPropertyName("output_downbeat_count")]
    public int OutputDownbeatCount { get; init; }

    [JsonPropertyName("sanitized")]
    public bool Sanitized { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("max_distance_to_nearest_beat_seconds")]
    public double? MaxDistanceToNearestBeatSeconds { get; init; }

    [JsonPropertyName("max_allowed_distance_seconds")]
    public double MaxAllowedDistanceSeconds { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];

    [JsonIgnore]
    public IReadOnlyList<double> Downbeats { get; init; } = [];
}
