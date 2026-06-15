namespace EternalLoop.BranchAnalysis.Core.Application;

public interface IBranchAnalysisService
{
    Task<BranchAnalysisResult> AnalyzeAsync(
        BranchAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
