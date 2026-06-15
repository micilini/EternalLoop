using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class StructuralPolicySummary
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = StructuralBranchPolicy.PolicyName;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("shortBranchPolicy")]
    public string ShortBranchPolicy { get; init; } = string.Empty;

    [JsonPropertyName("antiLocalLoopPolicy")]
    public bool AntiLocalLoopPolicy { get; init; }

    [JsonPropertyName("minVeryShortJumpBeats")]
    public int? MinVeryShortJumpBeats { get; init; }

    [JsonPropertyName("minShortJumpBeats")]
    public int? MinShortJumpBeats { get; init; }

    [JsonPropertyName("phraseWindowBeats")]
    public int? PhraseWindowBeats { get; init; }
}
