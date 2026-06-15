using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Application;

public sealed record AnalysisEngineResult
{
    public AnalysisEngineResult(TrackAnalysis analysis)
    {
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        Summary = AnalysisEngineSummary.From(analysis);
    }

    public TrackAnalysis Analysis { get; }

    public AnalysisEngineSummary Summary { get; }
}
