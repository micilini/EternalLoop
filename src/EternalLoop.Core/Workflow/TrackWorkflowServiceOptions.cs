using EternalLoop.Core.Settings;
using EternalLoop.Core.Cache;
using EternalLoop.Core.Diagnostics;

namespace EternalLoop.Core.Workflow;

public sealed record TrackWorkflowServiceOptions
{
    public string WorkspaceRoot { get; init; } = Path.Combine(
        Path.GetTempPath(),
        "EternalLoop",
        "workflows");

    public bool ForceIntermediateExports { get; init; } = true;

    public bool PrettyIntermediateExports { get; init; } = true;

    public LoopTuningSettings Tuning { get; init; } = LoopTuningSettings.Balanced();

    public int SettingsSchemaVersion { get; init; } = 4;

    public TrackFileIdentityService? FileIdentityService { get; init; }

    public TrackRuntimePackageCacheService? RuntimePackageCacheService { get; init; }

    public IAppLogger? Logger { get; init; }

    public static TrackWorkflowServiceOptions Default { get; } = new();
}
