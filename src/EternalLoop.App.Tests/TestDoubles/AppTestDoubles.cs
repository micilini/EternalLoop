using EternalLoop.Core.Diagnostics;

namespace EternalLoop.App.Tests.TestDoubles;

internal static class AsyncTest
{
    public static async Task EventuallyAsync(Action assertion)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            try
            {
                assertion();
                return;
            }
            catch (Exception exception)
            {
                lastException = exception;
                await Task.Delay(25);
            }
        }

        throw lastException ?? new TimeoutException("Condition was not met.");
    }
}

internal sealed class RecordingAppLogger : IAppLogger
{
    public List<LogEntry> Entries { get; } = [];

    public void Log(AppLogLevel level, string message, Exception? exception = null)
    {
        Entries.Add(new LogEntry(level, message, exception));
    }
}

internal sealed record LogEntry(AppLogLevel Level, string Message, Exception? Exception);
