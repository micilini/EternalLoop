using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

public sealed class BeatGridCandidateDiagnostics
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("candidate_count")]
    public int CandidateCount { get; init; }

    [JsonPropertyName("selected_candidate_id")]
    public string? SelectedCandidateId { get; init; }

    [JsonPropertyName("selected_source")]
    public BeatGridCandidateSourceKind SelectedSource { get; init; } = BeatGridCandidateSourceKind.Unknown;

    [JsonPropertyName("selection_reason")]
    public string SelectionReason { get; init; } = "not-evaluated";

    [JsonPropertyName("legacy_candidate_id")]
    public string? LegacyCandidateId { get; init; }

    [JsonPropertyName("advisor_candidate_id")]
    public string? AdvisorCandidateId { get; init; }

    [JsonPropertyName("advisor_available")]
    public bool AdvisorAvailable { get; init; }

    [JsonPropertyName("advisor_accepted_as_candidate")]
    public bool AdvisorAcceptedAsCandidate { get; init; }

    [JsonPropertyName("advisor_rejection_reason")]
    public string? AdvisorRejectionReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
