namespace EternalLoop.Playback.Audio;

public sealed class BeatChangedEventArgs : EventArgs
{
    public int BeatIndex { get; init; }

    public double BeatStartSeconds { get; init; }

    public double BeatDurationSeconds { get; init; }
}
