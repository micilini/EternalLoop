namespace EternalLoop.BranchAnalysis.Core.IO;

public sealed class AnalysisDiscoveryResult
{
    public required string Name { get; init; }
    public required string DirectoryPath { get; init; }
    public required string AnalysisPath { get; init; }
}
