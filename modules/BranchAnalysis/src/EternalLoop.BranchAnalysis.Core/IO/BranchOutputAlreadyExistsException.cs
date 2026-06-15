namespace EternalLoop.BranchAnalysis.Core.IO;

public sealed class BranchOutputAlreadyExistsException : Exception
{
    public BranchOutputAlreadyExistsException(string outputPath)
        : base($"Branch output already exists: {outputPath}")
    {
        OutputPath = outputPath;
    }

    public string OutputPath { get; }
}
