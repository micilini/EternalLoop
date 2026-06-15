namespace EternalLoop.AnalysisEngine.Core.Models;

public sealed class Tatum
{
    public required int Index { get; init; }

    public required double Start { get; init; }

    public required double Duration { get; init; }

    public required double Confidence { get; init; }
}
