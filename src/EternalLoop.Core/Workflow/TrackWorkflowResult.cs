using EternalLoop.Core.Runtime;

namespace EternalLoop.Core.Workflow;

public sealed record TrackWorkflowResult
{
    private TrackWorkflowResult(
        TrackWorkflowStatus status,
        TrackInput input,
        TrackAnalysisSummary? analysisSummary,
        TrackBranchSummary? branchSummary,
        TrackRuntimePreparationSummary? runtimeSummary,
        TrackRuntimePackage? runtimePackage,
        TrackWorkflowError? error,
        bool cacheHit,
        string analysisSource)
    {
        Status = status;
        Input = input ?? throw new ArgumentNullException(nameof(input));
        AnalysisSummary = analysisSummary;
        BranchSummary = branchSummary;
        RuntimePackage = runtimePackage;
        RuntimeSummary = runtimePackage?.Summary ?? runtimeSummary;
        Error = error;
        CacheHit = cacheHit;
        AnalysisSource = analysisSource;
    }

    public TrackWorkflowStatus Status { get; }

    public TrackInput Input { get; }

    public TrackAnalysisSummary? AnalysisSummary { get; }

    public TrackBranchSummary? BranchSummary { get; }

    public TrackRuntimePreparationSummary? RuntimeSummary { get; }

    public TrackRuntimePackage? RuntimePackage { get; }

    public TrackWorkflowError? Error { get; }

    public bool CacheHit { get; }

    public string AnalysisSource { get; }

    public bool IsSuccess => Status == TrackWorkflowStatus.Completed && Error is null;

    public static TrackWorkflowResult Completed(
        TrackInput input,
        TrackAnalysisSummary analysisSummary,
        TrackBranchSummary branchSummary,
        TrackRuntimePreparationSummary? runtimeSummary = null)
    {
        return new TrackWorkflowResult(
            TrackWorkflowStatus.Completed,
            input,
            analysisSummary,
            branchSummary,
            runtimeSummary,
            runtimePackage: null,
            error: null,
            cacheHit: false,
            analysisSource: "Fresh analysis");
    }

    public static TrackWorkflowResult Completed(
        TrackInput input,
        TrackAnalysisSummary analysisSummary,
        TrackBranchSummary branchSummary,
        TrackRuntimePackage runtimePackage,
        bool cacheHit = false,
        string? analysisSource = null)
    {
        return new TrackWorkflowResult(
            TrackWorkflowStatus.Completed,
            input,
            analysisSummary,
            branchSummary,
            runtimeSummary: runtimePackage?.Summary,
            runtimePackage,
            error: null,
            cacheHit,
            analysisSource ?? (cacheHit ? "Cached analysis" : "Fresh analysis"));
    }

    public static TrackWorkflowResult Failed(
        TrackInput input,
        TrackWorkflowError error,
        TrackAnalysisSummary? analysisSummary = null,
        TrackBranchSummary? branchSummary = null)
    {
        return new TrackWorkflowResult(
            TrackWorkflowStatus.Failed,
            input,
            analysisSummary,
            branchSummary,
            runtimeSummary: null,
            runtimePackage: null,
            error,
            cacheHit: false,
            analysisSource: "Failed");
    }

    public static TrackWorkflowResult Canceled(TrackInput input)
    {
        return new TrackWorkflowResult(
            TrackWorkflowStatus.Canceled,
            input,
            analysisSummary: null,
            branchSummary: null,
            runtimeSummary: null,
            runtimePackage: null,
            error: null,
            cacheHit: false,
            analysisSource: "Canceled");
    }
}
