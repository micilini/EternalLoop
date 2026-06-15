using EternalLoop.Core.Diagnostics;

namespace EternalLoop.App.Diagnostics;

public sealed record UnhandledUiExceptionDecision(
    UnhandledUiExceptionAction Action,
    AppLogLevel LogLevel,
    string LogMessage,
    string UserMessage,
    string DialogTitle);
