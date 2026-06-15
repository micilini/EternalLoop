using EternalLoop.AnalysisEngine.Core.Progress;

namespace EternalLoop.Core.Workflow;

internal sealed class TrackWorkflowAnalysisProgressMapper
{
    private double _lastPercent = 10;

    public double Map(AnalysisStage stage, double progress01)
    {
        double clamped = Math.Clamp(progress01, 0, 1);

        (double start, double end) = stage switch
        {
            AnalysisStage.LoadingAudio => (10, 15),
            AnalysisStage.ExtractingFeatures => (15, 40),
            AnalysisStage.TrackingBeats => (40, 58),
            AnalysisStage.BuildingAnalysis => (58, 65),
            AnalysisStage.Validating => (65, 68),
            AnalysisStage.Done => (68, 68),
            _ => (10, 68)
        };

        double mapped = start + ((end - start) * clamped);

        if (mapped < _lastPercent)
        {
            mapped = _lastPercent;
        }

        _lastPercent = mapped;
        return mapped;
    }
}
