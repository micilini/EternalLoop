using EternalLoop.AnalysisEngine.Core.Application;
using EternalLoop.BranchAnalysis.Core.Application;

namespace EternalLoop.Core.Workflow;

public static class TrackWorkflowServiceFactory
{
    public static ITrackWorkflowService CreateDefault(
        TrackWorkflowServiceOptions? options = null)
    {
        return new TrackWorkflowService(
            AnalysisEngineServiceFactory.CreateDefault(),
            BranchAnalysisServiceFactory.CreateDefault(),
            options);
    }
}
