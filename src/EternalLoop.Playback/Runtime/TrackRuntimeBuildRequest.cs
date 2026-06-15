namespace EternalLoop.Playback.Runtime;

public sealed record TrackRuntimeBuildRequest
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Artist { get; init; } = "Local";

    public string AudioPath { get; init; } = string.Empty;

    public string AnalysisPath { get; init; } = string.Empty;

    public string BranchesPath { get; init; } = string.Empty;

    public double DurationSeconds { get; init; }

    public IReadOnlyList<RuntimeBeatInput> Beats { get; init; } = [];

    public IReadOnlyList<RuntimeBranchInput> ActiveBranches { get; init; } = [];

    public IReadOnlyList<RuntimeBranchInput> CandidateBranches { get; init; } = [];
}
