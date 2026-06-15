namespace EternalLoop.AnalysisEngine.Core.Models;

public sealed record LoadedAudio(
    float[] Samples,
    int SampleRate,
    double DurationSeconds,
    string FileHash,
    string FilePath,
    string FileName);
