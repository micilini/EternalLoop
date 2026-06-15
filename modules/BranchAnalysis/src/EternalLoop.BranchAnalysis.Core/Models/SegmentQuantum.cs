using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public sealed class SegmentQuantum : TimeQuantum
{
    [JsonPropertyName("loudness_start")]
    public double LoudnessStart { get; set; }

    [JsonPropertyName("loudness_max")]
    public double LoudnessMax { get; set; }

    [JsonPropertyName("loudness_max_time")]
    public double LoudnessMaxTime { get; set; }

    [JsonPropertyName("pitches")]
    public List<double> Pitches { get; set; } = [];

    [JsonPropertyName("timbre")]
    public List<double> Timbre { get; set; } = [];
}
