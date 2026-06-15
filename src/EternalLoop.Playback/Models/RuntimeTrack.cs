namespace EternalLoop.Playback.Models;

public sealed class RuntimeTrack
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Artist { get; init; }

    public required string AudioPath { get; init; }

    public string AnalysisPath { get; init; } = string.Empty;

    public string BranchesPath { get; init; } = string.Empty;

    public double DurationSeconds { get; init; }

    public required IReadOnlyList<RuntimeBeat> Beats { get; init; }

    public int ActiveBranchCount { get; set; }

    public int CandidateBranchCount { get; set; }
}
