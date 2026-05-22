using EternalLoop.Contracts.Enums;

namespace EternalLoop.Contracts.Abstractions;

public interface IAnalysisProgressReporter
{
    void Report(AnalysisStage stage, double progress01, string? message = null);
}
