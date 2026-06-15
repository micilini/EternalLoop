using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public sealed class TrackInfo
{
    [JsonPropertyName("service")]
    public string? Service { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }
}
