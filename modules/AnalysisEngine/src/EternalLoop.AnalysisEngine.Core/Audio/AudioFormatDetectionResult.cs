namespace EternalLoop.AnalysisEngine.Core.Audio;

public sealed record AudioFormatDetectionResult(
    AudioFileFormat Format,
    string Extension,
    bool IsSupported);
