using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using EternalLoop.App.Controls;
using FluentAssertions;

namespace EternalLoop.App.Tests.Controls;

public sealed class MiniSpectrumControlDisposalTests
{
    [Fact]
    public async Task Unloaded_stops_internal_timer()
    {
        await RunStaAsync(() =>
        {
            var control = new MiniSpectrumControl();
            var timer = (DispatcherTimer)typeof(MiniSpectrumControl)
                .GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(control)!;

            control.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            timer.IsEnabled.Should().BeTrue();

            control.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            timer.IsEnabled.Should().BeFalse();
        });
    }

    private static Task RunStaAsync(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
