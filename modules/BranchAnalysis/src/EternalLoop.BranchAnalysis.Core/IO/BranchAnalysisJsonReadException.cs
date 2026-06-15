namespace EternalLoop.BranchAnalysis.Core.IO;

public sealed class BranchAnalysisJsonReadException : Exception
{
    public BranchAnalysisJsonReadException(string filePath, Exception innerException)
        : base($"Failed to read JSON file: {filePath}. {innerException.Message}", innerException)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}
