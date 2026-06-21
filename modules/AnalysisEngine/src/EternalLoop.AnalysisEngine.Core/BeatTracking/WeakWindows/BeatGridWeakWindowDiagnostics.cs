using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindowDiagnostics
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "not-run";

    [JsonPropertyName("window_count")]
    public int WindowCount { get; init; }

    [JsonPropertyName("weak_window_count")]
    public int WeakWindowCount { get; init; }

    [JsonPropertyName("promising_advisor_window_count")]
    public int PromisingAdvisorWindowCount { get; init; }

    [JsonPropertyName("future_correction_candidate_count")]
    public int FutureCorrectionCandidateCount { get; init; }

    [JsonPropertyName("blocked_window_count")]
    public int BlockedWindowCount { get; init; }

    [JsonPropertyName("future_correction_readiness")]
    public string FutureCorrectionReadiness { get; init; } = "not-ready";

    [JsonPropertyName("future_correction_ready")]
    public bool FutureCorrectionReady { get; init; }

    [JsonPropertyName("should_modify_final_grid")]
    public bool ShouldModifyFinalGrid { get; init; } = false;

    [JsonPropertyName("should_select_advisor")]
    public bool ShouldSelectAdvisor { get; init; } = false;

    [JsonPropertyName("should_apply_correction")]
    public bool ShouldApplyCorrection { get; init; } = false;

    [JsonPropertyName("external_benchmark_claim_status")]
    public string ExternalBenchmarkClaimStatus { get; init; } = "not-evaluated";

    [JsonPropertyName("windows")]
    public IReadOnlyList<BeatGridWeakWindow> Windows { get; init; } = [];

    [JsonPropertyName("unreliable_reason")]
    public string? UnreliableReason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static BeatGridWeakWindowDiagnostics NotAvailable(string reason)
    {
        return new BeatGridWeakWindowDiagnostics
        {
            Enabled = true,
            Status = "not-available",
            UnreliableReason = reason,
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Notes = [reason]
        };
    }
}
