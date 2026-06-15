namespace EternalLoop.AnalysisEngine.Core.Progress;

public interface IAnalysisProgressReporter
{
    void Report(AnalysisStage stage, double progress01, string? message = null);
}
