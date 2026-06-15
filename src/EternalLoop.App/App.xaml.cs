using System.Windows;
using System.Windows.Threading;
using EternalLoop.App.Diagnostics;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Settings;
using EternalLoop.App.Views;

namespace EternalLoop.App;

public partial class App : Application
{
    public static IAppLogger Logger { get; private set; } = NullAppLogger.Instance;

    public App()
    {
        var pathProvider = new AppPathProvider();
        Logger = new FileAppLogger(pathProvider);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Logger.Log(AppLogLevel.Information, "EternalLoop app startup.");

        var splashScreen = new SplashScreenWindow();
        splashScreen.Show();
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        UnhandledUiExceptionDecision decision =
            UnhandledUiExceptionPolicy.Decide(e.Exception);

        Logger.Log(decision.LogLevel, decision.LogMessage, e.Exception);

        MessageBox.Show(
            decision.UserMessage,
            decision.DialogTitle,
            MessageBoxButton.OK,
            decision.Action == UnhandledUiExceptionAction.Continue
                ? MessageBoxImage.Warning
                : MessageBoxImage.Error);

        e.Handled = true;

        if (decision.Action == UnhandledUiExceptionAction.Continue)
        {
            return;
        }

        DisposeOpenWindowDataContexts();
        Current.Shutdown(1);
    }

    private static void DisposeOpenWindowDataContexts()
    {
        foreach (Window window in Current.Windows)
        {
            if (window.DataContext is not IDisposable disposable)
            {
                continue;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Log(
                    AppLogLevel.Critical,
                    "DataContext disposal failed during fatal UI exception shutdown.",
                    exception);
            }
        }
    }

    private static void OnUnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        Logger.Log(
            AppLogLevel.Critical,
            "Unhandled app domain exception.",
            e.ExceptionObject as Exception);
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        Logger.Log(AppLogLevel.Error, "Unobserved task exception.", e.Exception);
        e.SetObserved();
    }
}
