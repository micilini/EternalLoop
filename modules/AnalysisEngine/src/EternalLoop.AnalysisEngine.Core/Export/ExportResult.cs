namespace EternalLoop.AnalysisEngine.Core.Export;

public sealed class ExportResult
{
    public required string FilePath { get; init; }

    public required long BytesWritten { get; init; }
}
