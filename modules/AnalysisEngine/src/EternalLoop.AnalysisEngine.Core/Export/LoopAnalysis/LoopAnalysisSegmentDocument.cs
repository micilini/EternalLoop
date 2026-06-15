using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;

public sealed class LoopAnalysisSegmentDocument
{
    [JsonPropertyName("start")]
    public required double Start { get; init; }

    [JsonPropertyName("duration")]
    public required double Duration { get; init; }

    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }

    [JsonPropertyName("loudness_start")]
    public required double LoudnessStart { get; init; }

    [JsonPropertyName("loudness_max")]
    public required double LoudnessMax { get; init; }

    [JsonPropertyName("loudness_max_time")]
    public required double LoudnessMaxTime { get; init; }

    [JsonPropertyName("pitches")]
    public required float[] Pitches { get; init; }

    [JsonPropertyName("timbre")]
    public required float[] Timbre { get; init; }
}
