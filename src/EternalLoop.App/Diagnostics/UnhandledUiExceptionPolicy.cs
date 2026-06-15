using System.IO;
using System.Windows.Markup;
using EternalLoop.Core.Diagnostics;

namespace EternalLoop.App.Diagnostics;

public static class UnhandledUiExceptionPolicy
{
    private const string RecoverableUserMessage =
        "EternalLoop found a recoverable problem and logged the details. You can keep using the app. If something looks wrong, restart it.";

    private const string FatalUserMessage =
        "EternalLoop found a serious problem and needs to close safely. The details were saved to the log.";

    public static UnhandledUiExceptionDecision Decide(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return IsRecoverable(exception)
            ? new UnhandledUiExceptionDecision(
                UnhandledUiExceptionAction.Continue,
                AppLogLevel.Warning,
                "Recoverable UI exception.",
                RecoverableUserMessage,
                "EternalLoop")
            : new UnhandledUiExceptionDecision(
                UnhandledUiExceptionAction.Shutdown,
                AppLogLevel.Critical,
                "Fatal UI exception.",
                FatalUserMessage,
                "EternalLoop needs to close");
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is OperationCanceledException
            or IOException
            or UnauthorizedAccessException;
    }
}
