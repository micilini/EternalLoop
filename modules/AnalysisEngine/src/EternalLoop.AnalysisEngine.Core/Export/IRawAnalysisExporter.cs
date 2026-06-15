using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Export;

public interface IRawAnalysisExporter
{
    Task<ExportResult> ExportAsync(
        TrackAnalysis analysis,
        string outputDirectory,
        bool force,
        bool pretty,
        CancellationToken cancellationToken);
}
