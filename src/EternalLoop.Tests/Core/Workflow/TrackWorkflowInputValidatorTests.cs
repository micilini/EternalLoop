using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackWorkflowInputValidatorTests
{
    [Theory]
    [InlineData(".ogg")]
    [InlineData(".flac")]
    public void ValidateShouldRejectFormerRuntimeExtensions(string extension)
    {
        TrackWorkflowError? error = TrackWorkflowInputValidator.Validate(
            TrackInput.FromFilePath(Path.Combine(Path.GetTempPath(), $"track{extension}")));

        error.Should().NotBeNull();
        error!.Code.Should().Be("unsupported_audio_format");
        error.Message.Should().Be("Choose an MP3, WAV, M4A or AAC file.");
    }

    [Theory]
    [InlineData(".mp3")]
    [InlineData(".wav")]
    [InlineData(".m4a")]
    [InlineData(".aac")]
    public async Task ValidateShouldAcceptSupportedRuntimeExtensions(string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);

        try
        {
            TrackWorkflowError? error = TrackWorkflowInputValidator.Validate(TrackInput.FromFilePath(path));

            error.Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidateShouldReturnUpdatedSupportedFormatMessage()
    {
        TrackWorkflowError? error = TrackWorkflowInputValidator.Validate(
            TrackInput.FromFilePath(Path.Combine(Path.GetTempPath(), "track.txt")));

        error.Should().NotBeNull();
        error!.Message.Should().Be("Choose an MP3, WAV, M4A or AAC file.");
        error.Message.Should().NotContain("FLAC");
        error.Message.Should().NotContain("OGG");
    }
}
