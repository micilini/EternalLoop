namespace EternalLoop.AnalysisEngine.Core.Progress;

public sealed class NullAnalysisProgressReporter : IAnalysisProgressReporter
{
    public static readonly NullAnalysisProgressReporter Instance = new();

    private NullAnalysisProgressReporter()
    {
    }

    public void Report(AnalysisStage stage, double progress01, string? message = null)
    {
    }
}
