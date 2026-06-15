using EternalLoop.App.Services;
using EternalLoop.App.ViewModels;
using FluentAssertions;
using System.IO;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class WelcomeViewModelTests
{
    [Fact]
    public void OpenFileCommandWithCancelledPickerShouldNotStartAnalysis()
    {
        bool started = false;
        var viewModel = new WelcomeViewModel(new FakeFilePickerService(null), _ => started = true);

        viewModel.OpenFileCommand.Execute(null);

        started.Should().BeFalse();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void OpenFileCommandWithSupportedFileShouldStartAnalysis()
    {
        string path = Path.Combine(Path.GetTempPath(), "track.mp3");
        string? startedPath = null;
        var viewModel = new WelcomeViewModel(new FakeFilePickerService(path), selected => startedPath = selected);

        viewModel.OpenFileCommand.Execute(null);

        startedPath.Should().Be(path);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void OpenFileCommandWithUnsupportedFileShouldShowFriendlyError()
    {
        bool started = false;
        var viewModel = new WelcomeViewModel(
            new FakeFilePickerService(Path.Combine(Path.GetTempPath(), "track.flac")),
            _ => started = true);

        viewModel.OpenFileCommand.Execute(null);

        started.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Choose an MP3, WAV, M4A or AAC file.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FileDroppedCommandWithNullOrBlankPathShouldDoNothingSafely(string? path)
    {
        bool started = false;
        var viewModel = new WelcomeViewModel(new FakeFilePickerService(null), _ => started = true);

        viewModel.FileDroppedCommand.Execute(path);

        started.Should().BeFalse();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SupportedFormatMessageShouldRemainRuntimeFormatTruth()
    {
        bool started = false;
        var viewModel = new WelcomeViewModel(new FakeFilePickerService("track.ogg"), _ => started = true);

        viewModel.OpenFileCommand.Execute(null);

        started.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Choose an MP3, WAV, M4A or AAC file.");
    }

    private sealed class FakeFilePickerService(string? path) : IFilePickerService
    {
        public string? PickAudioFile()
        {
            return path;
        }
    }
}
