using EternalLoop.Core.Settings;

namespace EternalLoop.Core.Diagnostics;

public sealed class FileAppLogger : IAppLogger
{
    private readonly IAppPathProvider _pathProvider;
    private readonly object _syncRoot = new();

    public FileAppLogger(IAppPathProvider pathProvider)
    {
        _pathProvider = pathProvider
            ?? throw new ArgumentNullException(nameof(pathProvider));
    }

    public void Log(AppLogLevel level, string message, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(_pathProvider.LogsDirectory);
            string logPath = Path.Combine(
                _pathProvider.LogsDirectory,
                $"eternalloop-{DateTime.UtcNow:yyyyMMdd}.log");
            string line = FormatLine(level, message, exception);

            lock (_syncRoot)
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
        catch
        {
        }
    }

    private static string FormatLine(
        AppLogLevel level,
        string message,
        Exception? exception)
    {
        string safeMessage = string.IsNullOrWhiteSpace(message)
            ? "(no message)"
            : message.ReplaceLineEndings(" ");

        if (exception is null)
        {
            return $"{DateTime.UtcNow:O} [{level}] {safeMessage}";
        }

        return $"{DateTime.UtcNow:O} [{level}] {safeMessage} " +
            $"{exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception.StackTrace}";
    }
}
