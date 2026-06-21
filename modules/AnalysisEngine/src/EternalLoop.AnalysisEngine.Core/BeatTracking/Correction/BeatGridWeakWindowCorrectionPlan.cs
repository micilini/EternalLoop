using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionPlan
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("mode")]
    public BeatGridWeakWindowCorrectionMode Mode { get; init; }

    [JsonPropertyName("window_count")]
    public int WindowCount { get; init; }

    [JsonPropertyName("candidate_window_count")]
    public int CandidateWindowCount { get; init; }

    [JsonPropertyName("accepted_window_count")]
    public int AcceptedWindowCount { get; init; }

    [JsonPropertyName("rejected_window_count")]
    public int RejectedWindowCount { get; init; }

    [JsonPropertyName("blocker_counts")]
    public IReadOnlyDictionary<string, int> BlockerCounts { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("top_blockers")]
    public IReadOnlyList<string> TopBlockers { get; init; } = [];

    [JsonPropertyName("windows")]
    public IReadOnlyList<BeatGridWeakWindowCorrectionWindow> Windows { get; init; } = [];

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];
}
