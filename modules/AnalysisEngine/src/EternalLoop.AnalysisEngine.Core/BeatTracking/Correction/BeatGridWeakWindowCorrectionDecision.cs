using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridWeakWindowCorrectionDecision
{
    Unknown = 0,
    NotEvaluated = 1,
    Rejected = 2,
    DiagnosticOnly = 3,
    CandidateCreated = 4,
    Blocked = 5
}
