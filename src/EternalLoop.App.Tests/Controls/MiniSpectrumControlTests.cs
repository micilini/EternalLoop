using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using EternalLoop.App.Controls;
using FluentAssertions;

namespace EternalLoop.App.Tests.Controls;

public sealed class MiniSpectrumControlTests
{
    [Fact]
    public async Task MiniSpectrumControlShouldExposeFrozenReusableBrushes()
    {
        await RunStaAsync(() =>
        {
            LinearGradientBrush playingBrush = GetStaticBrush("PlayingBarBrush");
            LinearGradientBrush idleBrush = GetStaticBrush("IdleBarBrush");

            playingBrush.IsFrozen.Should().BeTrue();
            idleBrush.IsFrozen.Should().BeTrue();
            GetStaticBrush("PlayingBarBrush").Should().BeSameAs(playingBrush);
            GetStaticBrush("IdleBarBrush").Should().BeSameAs(idleBrush);

            playingBrush.StartPoint.Should().Be(new Point(0, 1));
            playingBrush.EndPoint.Should().Be(new Point(0, 0));
            idleBrush.StartPoint.Should().Be(new Point(0, 1));
            idleBrush.EndPoint.Should().Be(new Point(0, 0));

            AssertGradientStops(playingBrush);
            AssertGradientStops(idleBrush);
        });
    }

    [Fact]
    public async Task MiniSpectrumControlShouldPreservePlayingAndIdleOpacity()
    {
        await RunStaAsync(() =>
        {
            GetStaticBrush("PlayingBarBrush").Opacity.Should().Be(0.92);
            GetStaticBrush("IdleBarBrush").Opacity.Should().Be(0.42);
        });
    }

    [Fact]
    public void MiniSpectrumControlShouldNotCreateGradientBrushInsideOnRender()
    {
        string onRender = ExtractOnRenderMethod();

        onRender.Should().NotContain("new LinearGradientBrush");
        onRender.Should().NotContain("new GradientStop");
        onRender.Should().NotContain(".Freeze()");
        onRender.Should().Contain("Brush barBrush = IsPlaying ? PlayingBarBrush : IdleBarBrush;");
        onRender.Should().Contain("drawingContext.DrawRoundedRectangle(barBrush, null");
    }

    [Fact]
    public async Task MiniSpectrumControlShouldStopTimerOnUnloaded()
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

    private static LinearGradientBrush GetStaticBrush(string fieldName)
    {
        return (LinearGradientBrush)typeof(MiniSpectrumControl)
            .GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
    }

    private static void AssertGradientStops(LinearGradientBrush brush)
    {
        brush.GradientStops.Should().HaveCount(3);
        brush.GradientStops[0].Color.Should().Be(Color.FromRgb(124, 231, 255));
        brush.GradientStops[0].Offset.Should().Be(0);
        brush.GradientStops[1].Color.Should().Be(Color.FromRgb(156, 108, 255));
        brush.GradientStops[1].Offset.Should().Be(0.55);
        brush.GradientStops[2].Color.Should().Be(Color.FromRgb(255, 119, 200));
        brush.GradientStops[2].Offset.Should().Be(1);
    }

    private static string ExtractOnRenderMethod()
    {
        string source = File.ReadAllText(FindRepositoryFile("src/EternalLoop.App/Controls/MiniSpectrumControl.cs"));
        const string startMarker = "protected override void OnRender";
        const string endMarker = "private void OnLoaded";
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        int end = source.IndexOf(endMarker, StringComparison.Ordinal);

        start.Should().BeGreaterThanOrEqualTo(0);
        end.Should().BeGreaterThan(start);

        return source[start..end];
    }

    private static string FindRepositoryFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string path = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file: {relativePath}");
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
