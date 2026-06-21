using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridWeakWindowCandidateStrength
{
    Unknown = 0,
    Weak = 1,
    Moderate = 2,
    Strong = 3,
    VeryStrong = 4
}
