namespace EternalLoop.BranchAnalysis.Core.IO;

public sealed class AnalysisRootNotFoundException : Exception
{
    public AnalysisRootNotFoundException(string analysisRoot)
        : base($"Analysis root does not exist: {analysisRoot}")
    {
        AnalysisRoot = analysisRoot;
    }

    public string AnalysisRoot { get; }
}
