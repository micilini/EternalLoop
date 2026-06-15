using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public sealed class AudioSummary
{
    [JsonPropertyName("duration")]
    public double Duration { get; set; }
}
