namespace EternalLoop.Playback.Runtime;

public sealed class RuntimeBuildException : Exception
{
    public RuntimeBuildException(string message)
        : base(message)
    {
    }

    public RuntimeBuildException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
