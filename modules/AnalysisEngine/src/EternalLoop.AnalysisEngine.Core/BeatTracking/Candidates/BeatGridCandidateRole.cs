using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridCandidateRole
{
    Unknown = 0,
    SafeAuthority = 1,
    Advisor = 2,
    SelectedFinal = 3,
    DiagnosticOnly = 4,
    CorrectedExperimental = 5
}
