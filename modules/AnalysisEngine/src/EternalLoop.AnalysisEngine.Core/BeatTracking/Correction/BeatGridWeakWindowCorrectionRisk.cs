using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridWeakWindowCorrectionRisk
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Blocked = 4
}
