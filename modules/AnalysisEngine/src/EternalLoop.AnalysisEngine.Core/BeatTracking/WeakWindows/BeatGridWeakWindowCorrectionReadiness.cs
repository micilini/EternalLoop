using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridWeakWindowCorrectionReadiness
{
    None = 0,
    DiagnosticOnly = 1,
    CandidateForReview = 2,
    CandidateForExperimentalCorrection = 3,
    Blocked = 4
}
