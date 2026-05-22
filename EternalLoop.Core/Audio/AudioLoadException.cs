namespace EternalLoop.Core.Audio;

public sealed class AudioLoadException : Exception
{
    public AudioLoadException(string message)
        : base(message)
    {
    }

    public AudioLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
