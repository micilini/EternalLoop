using System.Text.Json.Serialization;

namespace EternalLoop.Playback.Models;

public sealed class AudioSummary
{
    [JsonPropertyName("duration")]
    public double? Duration { get; init; }
}
