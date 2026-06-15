using EternalLoop.AnalysisEngine.Core.Progress;

namespace EternalLoop.AnalysisEngine.Cli;

public sealed class ConsoleAnalysisProgressReporter : IAnalysisProgressReporter
{
    private readonly bool _quiet;
    private readonly TextWriter _writer;

    public ConsoleAnalysisProgressReporter(bool quiet, TextWriter? writer = null)
    {
        _quiet = quiet;
        _writer = writer ?? Console.Out;
    }

    public void Report(AnalysisStage stage, double progress01, string? message = null)
    {
        if (_quiet)
        {
            return;
        }

        var text = string.IsNullOrWhiteSpace(message)
            ? $"[{stage}] {progress01:P0}"
            : $"[{stage}] {message}";

        _writer.WriteLine(text);
    }
}
