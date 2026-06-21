using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridPhaseAlignmentConfidence
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
