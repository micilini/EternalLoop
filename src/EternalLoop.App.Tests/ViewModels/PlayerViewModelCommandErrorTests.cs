using EternalLoop.Core.Diagnostics;
using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class PlayerViewModelCommandErrorTests
{
    [Fact]
    public async Task PlayPauseCommandShouldReportUnexpectedCommandFailure()
    {
        var logger = new RecordingLogger();
        var player = new PlayerViewModelDisposalTests.FakeLoopingAudioPlayer
        {
            PlayException = new ApplicationException("Unexpected playback failure.")
        };
        var viewModel = PlayerViewModelDisposalTests.CreateViewModel(
            playerFactory: new PlayerViewModelDisposalTests.FakePlayerFactory(player),
            logger: logger);

        await viewModel.InitializeAsync();

        Action execute = () => viewModel.PlayPauseCommand.Execute(null);

        execute.Should().NotThrow();
        await logger.WaitForLogAsync();

        viewModel.IsPlaying.Should().BeFalse();
        viewModel.AnalyzeAgainStatusText.Should().Be("Playback action failed. Try stopping and starting again.");
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == AppLogLevel.Error
            && entry.Message == "Playback command failed."
            && entry.Exception is ApplicationException);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        private readonly TaskCompletionSource _logged = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<LogEntry> Entries { get; } = [];

        public void Log(AppLogLevel level, string message, Exception? exception = null)
        {
            Entries.Add(new LogEntry(level, message, exception));
            _logged.TrySetResult();
        }

        public async Task WaitForLogAsync()
        {
            Task completed = await Task.WhenAny(_logged.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            completed.Should().BeSameAs(_logged.Task);
        }
    }

    private sealed record LogEntry(AppLogLevel Level, string Message, Exception? Exception);
}
