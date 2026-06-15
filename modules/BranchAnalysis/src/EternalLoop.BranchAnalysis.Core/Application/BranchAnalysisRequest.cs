using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Core.Application;

public sealed record BranchAnalysisRequest
{
    public BranchAnalysisRequest(
        string analysisPath,
        string outputRoot,
        string? analysisName = null,
        BranchAnalysisOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(analysisPath))
        {
            throw new ArgumentException("Branch analysis input path cannot be empty.", nameof(analysisPath));
        }

        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ArgumentException("Branch analysis output root cannot be empty.", nameof(outputRoot));
        }

        AnalysisPath = Path.GetFullPath(analysisPath);
        OutputRoot = Path.GetFullPath(outputRoot);
        AnalysisName = string.IsNullOrWhiteSpace(analysisName)
            ? ResolveAnalysisName(AnalysisPath)
            : analysisName;
        Options = options ?? BranchAnalysisOptions.CreateDefault();
    }

    public string AnalysisPath { get; }

    public string OutputRoot { get; }

    public string AnalysisName { get; }

    public BranchAnalysisOptions Options { get; }

    public string AnalysisDirectory => Path.GetDirectoryName(AnalysisPath) ?? string.Empty;

    private static string ResolveAnalysisName(string analysisPath)
    {
        string? directory = Path.GetDirectoryName(analysisPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            return Path.GetFileNameWithoutExtension(analysisPath);
        }

        string directoryName = Path.GetFileName(directory);

        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            return directoryName;
        }

        return Path.GetFileNameWithoutExtension(analysisPath);
    }
}
