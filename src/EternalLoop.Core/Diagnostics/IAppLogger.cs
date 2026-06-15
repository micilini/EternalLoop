namespace EternalLoop.Core.Diagnostics;

public interface IAppLogger
{
    void Log(AppLogLevel level, string message, Exception? exception = null);
}
