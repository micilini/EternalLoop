namespace EternalLoop.Core.Diagnostics;

public sealed class NullAppLogger : IAppLogger
{
    public static NullAppLogger Instance { get; } = new();

    private NullAppLogger()
    {
    }

    public void Log(AppLogLevel level, string message, Exception? exception = null)
    {
    }
}
