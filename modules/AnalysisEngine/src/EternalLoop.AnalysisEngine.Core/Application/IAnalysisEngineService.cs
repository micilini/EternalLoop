using EternalLoop.AnalysisEngine.Core.Progress;

namespace EternalLoop.AnalysisEngine.Core.Application;

public interface IAnalysisEngineService
{
    Task<AnalysisEngineResult> AnalyzeAsync(
        AnalysisEngineRequest request,
        IAnalysisProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default);
}
