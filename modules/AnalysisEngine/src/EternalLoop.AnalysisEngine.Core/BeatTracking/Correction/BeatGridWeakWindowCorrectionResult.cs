using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionResult
{
    [JsonPropertyName("corrected_candidate")]
    public BeatGridCandidate? CorrectedCandidate { get; init; }

    [JsonPropertyName("plan")]
    public required BeatGridWeakWindowCorrectionPlan Plan { get; init; }

    [JsonPropertyName("diagnostics")]
    public required BeatGridWeakWindowCorrectionDiagnostics Diagnostics { get; init; }
}
