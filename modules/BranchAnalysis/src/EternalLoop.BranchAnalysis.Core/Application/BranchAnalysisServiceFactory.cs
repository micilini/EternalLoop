namespace EternalLoop.BranchAnalysis.Core.Application;

public static class BranchAnalysisServiceFactory
{
    public static IBranchAnalysisService CreateDefault()
    {
        return new BranchAnalysisService();
    }
}
