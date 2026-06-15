namespace EternalLoop.AnalysisEngine.Core.Audio;

public sealed class AudioLoadingException : Exception
{
    public AudioLoadingException(string message)
        : base(message)
    {
    }

    public AudioLoadingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
