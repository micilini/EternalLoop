namespace EternalLoop.Playback.Runtime;

public sealed record RuntimeBeatInput(
    int Which,
    double Start,
    double Duration,
    double Confidence);
