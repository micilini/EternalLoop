using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

public sealed class BeatGridCandidate
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("source")]
    public required BeatGridCandidateSourceKind Source { get; init; }

    [JsonPropertyName("role")]
    public required BeatGridCandidateRole Role { get; init; }

    [JsonPropertyName("provider_name")]
    public required string ProviderName { get; init; }

    [JsonPropertyName("beat_grid_mode")]
    public string? BeatGridMode { get; init; }

    [JsonPropertyName("beat_times")]
    public required double[] BeatTimes { get; init; }

    [JsonPropertyName("downbeat_times")]
    public double[] DownbeatTimes { get; init; } = [];

    [JsonPropertyName("confidences")]
    public double[] Confidences { get; init; } = [];

    [JsonPropertyName("estimated_bpm")]
    public double? EstimatedBpm { get; init; }

    [JsonPropertyName("quality")]
    public required BeatGridCandidateQuality Quality { get; init; }

    [JsonPropertyName("created_by")]
    public string CreatedBy { get; init; } = "analysis-engine";

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
