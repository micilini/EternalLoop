namespace EternalLoop.AnalysisEngine.Core.Models;

public sealed class Beat
{
    public required int Index { get; init; }

    public required double Start { get; init; }

    public required double Duration { get; init; }

    public required double Confidence { get; init; }

    public required float[] Timbre { get; init; }

    public required float[] Pitches { get; init; }

    public required float[] Loudness { get; init; }

    public required float[] BarPosition { get; init; }
}
