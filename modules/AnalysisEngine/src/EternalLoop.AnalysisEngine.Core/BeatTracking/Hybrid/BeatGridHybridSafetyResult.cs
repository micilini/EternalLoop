using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

public sealed class BeatGridHybridSafetyResult
{
    [JsonPropertyName("is_safe")]
    public bool IsSafe { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static BeatGridHybridSafetyResult Safe(IReadOnlyList<string>? notes = null)
    {
        return new BeatGridHybridSafetyResult { IsSafe = true, Notes = notes ?? [] };
    }

    public static BeatGridHybridSafetyResult Unsafe(string reason, IReadOnlyList<string>? notes = null)
    {
        return new BeatGridHybridSafetyResult { IsSafe = false, Reason = reason, Notes = notes ?? [] };
    }
}
