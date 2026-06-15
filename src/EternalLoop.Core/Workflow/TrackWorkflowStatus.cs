namespace EternalLoop.Core.Workflow;

public enum TrackWorkflowStatus
{
    Idle,
    Queued,
    ValidatingInput,
    AnalyzingAudio,
    BuildingBranches,
    PreparingRuntime,
    Completed,
    Canceled,
    Failed
}
