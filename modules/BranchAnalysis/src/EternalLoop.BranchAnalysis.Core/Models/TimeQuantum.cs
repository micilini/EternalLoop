using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public class TimeQuantum
{
    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonIgnore]
    public TrackAnalysisDocument? Track { get; set; }

    [JsonIgnore]
    public int Which { get; set; } = -1;

    [JsonIgnore]
    public TimeQuantum? Prev { get; set; }

    [JsonIgnore]
    public TimeQuantum? Next { get; set; }

    [JsonIgnore]
    public TimeQuantum? Parent { get; set; }

    [JsonIgnore]
    public List<TimeQuantum> Children { get; set; } = [];

    [JsonIgnore]
    public int IndexInParent { get; set; } = -1;

    [JsonIgnore]
    public SegmentQuantum? Oseg { get; set; }

    [JsonIgnore]
    public List<SegmentQuantum> OverlappingSegments { get; set; } = [];

    [JsonIgnore]
    public List<BranchEdge> Neighbors { get; set; } = [];

    [JsonIgnore]
    public List<BranchEdge> AllNeighbors { get; set; } = [];

    [JsonIgnore]
    public double End => Start + Duration;

    [JsonIgnore]
    public double Reach { get; set; }
}
