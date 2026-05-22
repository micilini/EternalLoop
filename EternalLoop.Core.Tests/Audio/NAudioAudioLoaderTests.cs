using EternalLoop.Core.Audio;
using EternalLoop.Core.Tests.TestData;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace EternalLoop.Core.Tests.Audio;

public sealed class NAudioAudioLoaderTests
{
    [Fact]
    public async Task LoadAsync_Should_LoadMonoWav_AtTargetSampleRate()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateSineWaveFile(
            directory,
            "mono-22050.wav",
            sampleRate: 22_050,
            channels: 1,
            durationSeconds: 1.0);

        var loader = CreateLoader();

        var audio = await loader.LoadAsync(path, 22_050, CancellationToken.None);

        audio.SampleRate.Should().Be(22_050);
        audio.DurationSeconds.Should().BeApproximately(1.0, 0.05);
        audio.Samples.Should().NotBeEmpty();
        audio.Samples.Length.Should().BeInRange(22_000, 22_100);
        audio.FileHash.Should().HaveLength(64);
    }

    [Fact]
    public async Task LoadAsync_Should_ConvertStereoToMono()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateSineWaveFile(
            directory,
            "stereo-44100.wav",
            sampleRate: 44_100,
            channels: 2,
            durationSeconds: 1.0);

        var loader = CreateLoader();

        var audio = await loader.LoadAsync(path, 44_100, CancellationToken.None);

        audio.SampleRate.Should().Be(44_100);
        audio.DurationSeconds.Should().BeApproximately(1.0, 0.05);
        audio.Samples.Length.Should().BeInRange(44_000, 44_200);
    }

    [Fact]
    public async Task LoadAsync_Should_Resample_ToTargetSampleRate()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateSineWaveFile(
            directory,
            "stereo-44100.wav",
            sampleRate: 44_100,
            channels: 2,
            durationSeconds: 1.0);

        var loader = CreateLoader();

        var audio = await loader.LoadAsync(path, 22_050, CancellationToken.None);

        audio.SampleRate.Should().Be(22_050);
        audio.DurationSeconds.Should().BeApproximately(1.0, 0.05);
        audio.Samples.Length.Should().BeInRange(22_000, 22_100);
    }

    [Fact]
    public async Task LoadAsync_Should_ThrowAudioLoadException_WhenFileDoesNotExist()
    {
        var loader = CreateLoader();

        var act = () => loader.LoadAsync(
            Path.Combine(Path.GetTempPath(), "missing-eternalloop-file.wav"),
            22_050,
            CancellationToken.None);

        await act.Should().ThrowAsync<AudioLoadException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task LoadAsync_Should_ThrowAudioLoadException_ForUnsupportedExtension()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateTextFile(directory, "sample.txt");
        var loader = CreateLoader();

        var act = () => loader.LoadAsync(path, 22_050, CancellationToken.None);

        await act.Should().ThrowAsync<AudioLoadException>()
            .WithMessage("*Unsupported audio format*");
    }

    [Fact]
    public async Task LoadAsync_Should_RejectAudioLongerThanConfiguredLimit()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateSineWaveFile(
            directory,
            "too-long.wav",
            sampleRate: 22_050,
            channels: 1,
            durationSeconds: 1.0);

        var loader = CreateLoader(new AudioLoaderOptions
        {
            MaxDuration = TimeSpan.FromMilliseconds(500)
        });

        var act = () => loader.LoadAsync(path, 22_050, CancellationToken.None);

        await act.Should().ThrowAsync<AudioLoadException>()
            .WithMessage("*maximum allowed duration*");
    }

    private static NAudioAudioLoader CreateLoader(AudioLoaderOptions? options = null)
    {
        return new NAudioAudioLoader(Options.Create(options ?? new AudioLoaderOptions()));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "EternalLoopTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
