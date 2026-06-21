using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Options;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HybridCalibrationProfile
{
    StrictProduction = 0,
    BalancedProbe = 1,
    ExploratoryProbe = 2
}
