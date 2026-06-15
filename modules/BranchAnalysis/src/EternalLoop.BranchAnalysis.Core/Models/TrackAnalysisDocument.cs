using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public sealed class TrackAnalysisDocument
{
    [JsonPropertyName("info")]
    public TrackInfo Info { get; set; } = new();

    [JsonPropertyName("analysis")]
    public AnalysisData Analysis { get; set; } = new();

    [JsonPropertyName("audio_summary")]
    public AudioSummary AudioSummary { get; set; } = new();

    [JsonIgnore]
    public string? FixedTitle { get; set; }
}
