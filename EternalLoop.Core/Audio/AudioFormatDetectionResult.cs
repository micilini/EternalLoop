namespace EternalLoop.Core.Audio;

public sealed record AudioFormatDetectionResult(
    AudioFileFormat Format,
    string Extension,
    bool IsSupported);
