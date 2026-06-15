using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Core.Progress;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public interface ITrackAnalysisPipeline
{
    Task<TrackAnalysis> AnalyzeAsync(
        string inputPath,
        AnalysisOptions options,
        IAnalysisProgressReporter progressReporter,
        CancellationToken cancellationToken);
}
