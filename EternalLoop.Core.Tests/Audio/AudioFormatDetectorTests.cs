using EternalLoop.Core.Audio;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Audio;

public sealed class AudioFormatDetectorTests
{
    [Fact]
    public void Detect_Should_DetectWavFile()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateSineWaveFile(directory, "sample.wav");

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Wav);
        result.IsSupported.Should().BeTrue();
        result.Extension.Should().Be(".wav");
    }

    [Fact]
    public void Detect_Should_ReturnUnknown_ForUnsupportedExtension()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateTextFile(directory, "sample.txt");

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Unknown);
        result.IsSupported.Should().BeFalse();
    }

    [Theory]
    [InlineData("song.mp3")]
    [InlineData("song.flac")]
    [InlineData("song.m4a")]
    [InlineData("song.aac")]
    public void Detect_Should_AcceptSupportedExtensions(string fileName)
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateTextFile(directory, fileName);

        var result = AudioFormatDetector.Detect(path);

        result.IsSupported.Should().BeTrue();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "EternalLoopTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
