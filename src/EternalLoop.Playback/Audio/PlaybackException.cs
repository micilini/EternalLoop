namespace EternalLoop.Playback.Audio;

public sealed class PlaybackException : Exception
{
    public PlaybackException(string message)
        : base(message)
    {
    }

    public PlaybackException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
