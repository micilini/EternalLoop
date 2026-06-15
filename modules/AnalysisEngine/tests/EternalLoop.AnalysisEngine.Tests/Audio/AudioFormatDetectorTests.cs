using EternalLoop.AnalysisEngine.Core.Audio;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Audio;

public sealed class AudioFormatDetectorTests
{
    [Theory]
    [InlineData("sample.m4a", AudioFileFormat.M4A)]
    [InlineData("sample.aac", AudioFileFormat.Aac)]
    [InlineData("sample.mp4", AudioFileFormat.Mp4)]
    public void Detect_detects_supported_aac_family_by_extension(string fileName, AudioFileFormat expected)
    {
        var path = CreateFile(fileName, [0x00, 0x01, 0x02, 0x03]);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(expected);
        result.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void Detect_detects_ftyp_m4a_magic_bytes()
    {
        var path = CreateFile("sample.m4a", [0x00, 0x00, 0x00, 0x20, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'M', (byte)'4', (byte)'A', (byte)' ']);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.M4A);
        result.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void Detect_detects_ftyp_mp4_magic_bytes()
    {
        var path = CreateFile("sample.mp4", [0x00, 0x00, 0x00, 0x20, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'i', (byte)'s', (byte)'o', (byte)'m']);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Mp4);
        result.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void Detect_detects_aac_adts_magic_bytes()
    {
        var path = CreateFile("sample.aac", [0xFF, 0xF1, 0x50, 0x80]);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Aac);
        result.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void Detect_continues_detecting_wav()
    {
        var path = CreateFile("sample.wav", [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'A', (byte)'V', (byte)'E']);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Wav);
        result.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void Detect_continues_detecting_mp3_with_id3()
    {
        var path = CreateFile("sample.mp3", [(byte)'I', (byte)'D', (byte)'3', 0x04]);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Mp3);
        result.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void Detect_continues_detecting_mp3_frame_sync()
    {
        var path = CreateFile("sample.mp3", [0xFF, 0xFB, 0x90, 0x64]);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Mp3);
        result.IsSupported.Should().BeTrue();
    }

    [Fact]
    public void Detect_rejects_unknown_file()
    {
        var path = CreateFile("sample.bin", [0x12, 0x34, 0x56, 0x78]);

        var result = AudioFormatDetector.Detect(path);

        result.Format.Should().Be(AudioFileFormat.Unknown);
        result.IsSupported.Should().BeFalse();
    }

    private static string CreateFile(string fileName, byte[] bytes)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "EternalLoopAnalysisEngineTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
