using System.IO;
using FluentAssertions;

namespace EternalLoop.App.Tests.Diagnostics;

public sealed class AppExceptionHandlingTests
{
    [Fact]
    public void AppShouldUseUnhandledUiExceptionPolicy()
    {
        string content = ReadAppFile();

        content.Should().Contain("UnhandledUiExceptionPolicy.Decide(e.Exception)");
        content.Should().Contain("decision.Action == UnhandledUiExceptionAction.Continue");
        content.Should().Contain("Current.Shutdown(1)");
    }

    [Fact]
    public void AppShouldDisposeWindowDataContextsBeforeFatalShutdown()
    {
        string content = ReadAppFile();

        content.Should().Contain("DisposeOpenWindowDataContexts()");
        content.Should().Contain("window.DataContext is not IDisposable disposable");
        content.Should().Contain("disposable.Dispose()");
    }

    [Fact]
    public void AppShouldNotTreatEveryUiExceptionAsRecoverable()
    {
        string content = ReadAppFile();

        content.Should().NotContain(
            "EternalLoop found an unexpected problem and logged the details. You can keep using the app, but if something looks wrong, restart it.");
    }

    private static string ReadAppFile()
    {
        string repositoryRoot = FindRepositoryRoot();
        string path = Path.Combine(
            repositoryRoot,
            "src",
            "EternalLoop.App",
            "App.xaml.cs");

        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "EternalLoop.slnx");

            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
