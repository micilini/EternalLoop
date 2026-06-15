using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Progress;

namespace EternalLoop.AnalysisEngine.Core.Application;

public sealed class AnalysisEngineService : IAnalysisEngineService
{
    private readonly ITrackAnalysisPipeline _pipeline;

    public AnalysisEngineService(ITrackAnalysisPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public async Task<AnalysisEngineResult> AnalyzeAsync(
        AnalysisEngineRequest request,
        IAnalysisProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var analysis = await _pipeline
            .AnalyzeAsync(
                request.InputPath,
                request.Options,
                progressReporter ?? NullAnalysisProgressReporter.Instance,
                cancellationToken)
            .ConfigureAwait(false);

        return new AnalysisEngineResult(analysis);
    }
}
