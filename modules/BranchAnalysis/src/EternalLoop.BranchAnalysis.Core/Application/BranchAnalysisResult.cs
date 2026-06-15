using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Core.Application;

public sealed record BranchAnalysisResult
{
    public BranchAnalysisResult(BranchAnalysisItemResult itemResult)
    {
        ItemResult = itemResult ?? throw new ArgumentNullException(nameof(itemResult));
        Summary = BranchAnalysisSummary.From(itemResult);
    }

    public BranchAnalysisItemResult ItemResult { get; }

    public BranchAnalysisSummary Summary { get; }
}
