using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BeatGridAgreementConfidenceLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}
