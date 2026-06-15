namespace EternalLoop.Playback.Audio;

public sealed class LoadedAudio
{
    public required string SourcePath { get; init; }

    public required float[] Samples { get; init; }

    public int SampleRate { get; init; }

    public int Channels { get; init; }

    public double DurationSeconds { get; init; }

    public int TotalSampleFrames { get; init; }
}
