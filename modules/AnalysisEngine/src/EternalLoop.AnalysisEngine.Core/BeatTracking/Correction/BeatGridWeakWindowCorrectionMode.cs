using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridWeakWindowCorrectionMode
{
    Disabled = 0,
    DiagnosticsOnly = 1,
    ExperimentalCandidate = 2
}
