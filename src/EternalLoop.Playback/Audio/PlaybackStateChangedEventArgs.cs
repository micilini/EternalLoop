namespace EternalLoop.Playback.Audio;

public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackState State { get; init; }
}
