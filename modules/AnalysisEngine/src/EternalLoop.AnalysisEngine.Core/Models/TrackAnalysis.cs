using System.Text.Json.Serialization;
using EternalLoop.AnalysisEngine.Core.Analysis;

namespace EternalLoop.AnalysisEngine.Core.Models;

public sealed class TrackAnalysis
{
    public const string CurrentSchemaVersion = "analysis-exporter-0.1.0";

    public required TrackMetadata Metadata { get; init; }

    public required IReadOnlyList<Segment> Segments { get; init; }

    public required IReadOnlyList<Beat> Beats { get; init; }

    public required IReadOnlyList<Bar> Bars { get; init; }

    public required IReadOnlyList<Tatum> Tatums { get; init; }

    public required IReadOnlyList<Section> Sections { get; init; }

    public IReadOnlyList<object> MicroFingerprints { get; init; } = [];

    public object? Ai { get; init; }

    [JsonIgnore]
    public AnalysisDiagnostics? Diagnostics { get; init; }
}
