using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridWeakWindowRiskLevel
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Blocked = 4
}
