namespace EternalLoop.AnalysisEngine.Cli;

public sealed class AnalysisEngineArguments
{
    public required string InputPath { get; init; }

    public required string OutputDirectory { get; init; }

    public required string TrackId { get; init; }

    public required string Title { get; init; }

    public required string Artist { get; init; }

    public AnalysisEngineFormat Format { get; init; } = AnalysisEngineFormat.Both;

    public bool Pretty { get; init; } = true;

    public bool Force { get; init; }

    public bool Quiet { get; init; }

    public bool MusicalQuality { get; init; }

    public bool MusicalQualitySegmentation { get; init; }

    public bool MusicalQualityBeatMicroSnap { get; init; }

    public bool MusicalQualityTatums { get; init; }

    public bool MusicalQualitySections { get; init; }

    public bool MusicalQualityConfidences { get; init; }
}
