using System.IO;
using EternalLoop.App.Services;
using EternalLoop.App.ViewModels;
using FluentAssertions;

namespace EternalLoop.App.Tests.ViewModels;

public sealed class WelcomeViewModelAudioFormatTests
{
    [Theory]
    [InlineData("track.ogg")]
    [InlineData("track.flac")]
    public void DroppedFileShouldRejectFormerRuntimeExtensions(string fileName)
    {
        string? startedPath = null;
        var viewModel = new WelcomeViewModel(new FakeFilePickerService(null), path => startedPath = path);

        viewModel.FileDroppedCommand.Execute(Path.Combine(Path.GetTempPath(), fileName));

        startedPath.Should().BeNull();
        viewModel.ErrorMessage.Should().Be("Choose an MP3, WAV, M4A or AAC file.");
    }

    [Theory]
    [InlineData("track.mp3")]
    [InlineData("track.wav")]
    [InlineData("track.m4a")]
    [InlineData("track.aac")]
    public void DroppedFileShouldAcceptSupportedRuntimeExtensions(string fileName)
    {
        string? startedPath = null;
        string path = Path.Combine(Path.GetTempPath(), fileName);
        var viewModel = new WelcomeViewModel(new FakeFilePickerService(null), selectedPath => startedPath = selectedPath);

        viewModel.FileDroppedCommand.Execute(path);

        startedPath.Should().Be(path);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void FilePickerServiceShouldUseRuntimeSupportedDialogFilter()
    {
        string source = File.ReadAllText(FindRepositoryFile("src/EternalLoop.App/Services/FilePickerService.cs"));

        source.Should().Contain("SupportedAudioFormats.DialogFilter");
        source.Should().NotContain("*.ogg");
        source.Should().NotContain("*.flac");
    }

    private sealed class FakeFilePickerService(string? path) : IFilePickerService
    {
        public string? PickAudioFile()
        {
            return path;
        }
    }

    private static string FindRepositoryFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = System.IO.Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file: {relativePath}");
    }
}
