namespace EternalLoop.Playback.Models;

public sealed class RuntimeBeat
{
    public int Which { get; init; }

    public double Start { get; init; }

    public double Duration { get; init; }

    public double Confidence { get; init; }

    public RuntimeBeat? Prev { get; set; }

    public RuntimeBeat? Next { get; set; }

    public List<RuntimeBranchEdge> Neighbors { get; } = [];

    public List<RuntimeBranchEdge> AllNeighbors { get; } = [];
}
