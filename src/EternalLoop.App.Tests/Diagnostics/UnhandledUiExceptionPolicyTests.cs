using System.IO;
using System.Windows.Markup;
using EternalLoop.App.Diagnostics;
using EternalLoop.Core.Diagnostics;
using FluentAssertions;

namespace EternalLoop.App.Tests.Diagnostics;

public sealed class UnhandledUiExceptionPolicyTests
{
    [Theory]
    [MemberData(nameof(RecoverableExceptions))]
    public void DecideShouldContinueForRecoverableExceptions(Exception exception)
    {
        UnhandledUiExceptionDecision decision =
            UnhandledUiExceptionPolicy.Decide(exception);

        decision.Action.Should().Be(UnhandledUiExceptionAction.Continue);
        decision.LogLevel.Should().BeOneOf(AppLogLevel.Warning, AppLogLevel.Error);
        decision.UserMessage.Should().Contain("You can keep using the app");
        decision.DialogTitle.Should().Be("EternalLoop");
    }

    [Theory]
    [MemberData(nameof(FatalExceptions))]
    public void DecideShouldShutdownForFatalExceptions(Exception exception)
    {
        UnhandledUiExceptionDecision decision =
            UnhandledUiExceptionPolicy.Decide(exception);

        decision.Action.Should().Be(UnhandledUiExceptionAction.Shutdown);
        decision.LogLevel.Should().Be(AppLogLevel.Critical);
        decision.UserMessage.Should().NotContain("You can keep using the app");
        decision.DialogTitle.Should().Be("EternalLoop needs to close");
    }

    [Fact]
    public void DecideShouldRejectNullException()
    {
        Action action = () => UnhandledUiExceptionPolicy.Decide(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    public static TheoryData<Exception> RecoverableExceptions()
    {
        return new TheoryData<Exception>
        {
            new OperationCanceledException(),
            new IOException(),
            new UnauthorizedAccessException()
        };
    }

    public static TheoryData<Exception> FatalExceptions()
    {
        return new TheoryData<Exception>
        {
            new NullReferenceException(),
            new InvalidOperationException(),
            new ArgumentException(),
            new TypeInitializationException("TypeName", new InvalidOperationException()),
            new XamlParseException(),
            new OutOfMemoryException(),
            new BadImageFormatException(),
            new AccessViolationException(),
            new AppDomainUnloadedException(),
            new NotSupportedException(),
            new Exception()
        };
    }
}
