using EternalLoop.AnalysisEngine.Core.Progress;

namespace EternalLoop.Core.Workflow;

internal sealed class TrackWorkflowAnalysisProgressReporterAdapter : IAnalysisProgressReporter
{
    private readonly ITrackWorkflowProgressReporter? _progressReporter;
    private readonly TrackWorkflowAnalysisProgressMapper _mapper = new();

    public TrackWorkflowAnalysisProgressReporterAdapter(
        ITrackWorkflowProgressReporter? progressReporter)
    {
        _progressReporter = progressReporter;
    }

    public void Report(AnalysisStage stage, double progress01, string? message = null)
    {
        if (_progressReporter is null)
        {
            return;
        }

        double percent = _mapper.Map(stage, progress01);

        var progress = new TrackWorkflowProgress(
            TrackWorkflowStatus.AnalyzingAudio,
            string.IsNullOrWhiteSpace(message) ? CreateMessage(stage) : message,
            percent);

        _progressReporter
            .ReportAsync(progress)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    private static string CreateMessage(AnalysisStage stage)
    {
        return stage switch
        {
            AnalysisStage.LoadingAudio => "Loading audio.",
            AnalysisStage.ExtractingFeatures => "Extracting audio features.",
            AnalysisStage.TrackingBeats => "Tracking beats.",
            AnalysisStage.BuildingAnalysis => "Building audio analysis.",
            AnalysisStage.Validating => "Validating audio analysis.",
            AnalysisStage.Done => "Audio analysis completed.",
            _ => "Analyzing audio."
        };
    }
}
