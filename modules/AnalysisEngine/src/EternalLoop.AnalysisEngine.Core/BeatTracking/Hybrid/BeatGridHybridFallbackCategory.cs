using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridHybridFallbackCategory
{
    None = 0,

    RuntimeFailure = 1,
    AdvisorUnavailable = 2,
    AdvisorRejected = 3,

    CorrectedCandidateMissing = 10,
    CorrectedCandidateNotCreated = 11,
    NoCorrectionWindowsAccepted = 12,
    WeakWindowsNotReady = 13,
    CorrectionDiagnosticsMissing = 14,

    CorrectedCandidateUnsafe = 20,
    CorrectedCandidateDense = 21,
    CorrectedCandidateImplausible = 22,
    CorrectedCandidateCountRatioOutOfRange = 23,

    Unknown = 99
}
