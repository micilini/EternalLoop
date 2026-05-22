namespace EternalLoop.Contracts.Models;

public sealed record LoadedAudio(
    float[] Samples,
    int SampleRate,
    double DurationSeconds,
    string FileHash);
