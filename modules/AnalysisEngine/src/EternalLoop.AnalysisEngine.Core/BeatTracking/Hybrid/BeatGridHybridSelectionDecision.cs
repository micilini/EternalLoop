using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridHybridSelectionDecision
{
    Unknown = 0,
    SelectedLegacy = 1,
    SelectedCorrectedExperimental = 2,
    RejectedCorrectedCandidate = 3,
    FallbackToLegacy = 4,
    NotAvailable = 5
}
