using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.Application;

public sealed record AnalysisEngineRequest
{
    public AnalysisEngineRequest(string inputPath, AnalysisOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Analysis input path cannot be empty.", nameof(inputPath));
        }

        InputPath = Path.GetFullPath(inputPath);
        Options = options ?? new AnalysisOptions();
    }

    public string InputPath { get; }

    public AnalysisOptions Options { get; }
}
