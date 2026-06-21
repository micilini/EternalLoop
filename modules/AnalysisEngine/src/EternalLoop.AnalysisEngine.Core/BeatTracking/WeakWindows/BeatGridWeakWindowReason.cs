using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridWeakWindowReason
{
    Unknown = 0,
    LegacyTempoInstability = 1,
    LegacyIntervalOutlier = 2,
    LegacyLowConfidence = 3,
    LegacySparseOrDenseBeats = 4,
    AdvisorStrongerLocalAgreement = 5,
    AdvisorMoreStableIntervals = 6,
    CandidateDisagreement = 7,
    PhaseAlignmentUnstable = 8,
    AdvisorRejectedOrDense = 9,
    InsufficientEvidence = 10
}
