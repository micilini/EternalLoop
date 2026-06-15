using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public sealed class AnalysisData
{
    [JsonPropertyName("sections")]
    public List<TimeQuantum> Sections { get; set; } = [];

    [JsonPropertyName("bars")]
    public List<TimeQuantum> Bars { get; set; } = [];

    [JsonPropertyName("beats")]
    public List<TimeQuantum> Beats { get; set; } = [];

    [JsonPropertyName("tatums")]
    public List<TimeQuantum> Tatums { get; set; } = [];

    [JsonPropertyName("segments")]
    public List<SegmentQuantum> Segments { get; set; } = [];

    [JsonPropertyName("fsegments")]
    public List<SegmentQuantum> FilteredSegments { get; set; } = [];
}
