using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.Core.Settings;
using EternalLoop.Core.Workflow;

namespace EternalLoop.Core.Runtime;

public sealed record TrackRuntimePackageBuildRequest
{
    public required TrackInput Input { get; init; }

    public required TrackAnalysis Analysis { get; init; }

    public required string RunRoot { get; init; }

    public required string AnalysisJsonPath { get; init; }

    public required string BranchesJsonPath { get; init; }

    public required BranchExportPayload BranchPayload { get; init; }

    public required LoopTuningSettings Tuning { get; init; }

    public int SettingsSchemaVersion { get; init; } = 4;
}
