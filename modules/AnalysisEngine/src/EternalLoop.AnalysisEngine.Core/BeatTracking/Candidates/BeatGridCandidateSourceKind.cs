using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridCandidateSourceKind
{
    Unknown = 0,
    LegacyBuiltIn = 1,
    BeatThisAdvisor = 2,
    Hybrid = 3,
    WeakWindowCorrectedExperimental = 4
}
