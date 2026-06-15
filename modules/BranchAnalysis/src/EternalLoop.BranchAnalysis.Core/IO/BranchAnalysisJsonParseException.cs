namespace EternalLoop.BranchAnalysis.Core.IO;

public sealed class BranchAnalysisJsonParseException : Exception
{
    public BranchAnalysisJsonParseException(string filePath, Exception innerException)
        : base($"Failed to parse JSON file: {filePath}. {innerException.Message}", innerException)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}
