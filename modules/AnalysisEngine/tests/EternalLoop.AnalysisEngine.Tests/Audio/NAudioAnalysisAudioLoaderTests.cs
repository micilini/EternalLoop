using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Audio;

public sealed class NAudioAnalysisAudioLoaderTests
{
    [Fact]
    public async Task LoadAsync_loads_mono_wav_at_target_sample_rate()
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
        audio.FilePath.Should().Be(Path.GetFullPath(path));
        audio.FileName.Should().Be("mono-22050.wav");
    }

    [Fact]
    public async Task LoadAsync_converts_stereo_to_mono()
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
    public async Task LoadAsync_resamples_to_target_sample_rate()
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
    public async Task LoadAsync_rejects_missing_file()
    {
        var loader = CreateLoader();
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            "missing-eternalloop-analysis-exporter-file.wav");

        var act = () => loader.LoadAsync(missingPath, 22_050, CancellationToken.None);

        await act.Should().ThrowAsync<AudioLoadingException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task LoadAsync_rejects_unsupported_extension()
    {
        var directory = CreateTempDirectory();
        var path = TestWaveFileFactory.CreateTextFile(directory, "sample.txt");
        var loader = CreateLoader();

        var act = () => loader.LoadAsync(path, 22_050, CancellationToken.None);

        await act.Should().ThrowAsync<AudioLoadingException>()
            .WithMessage("*Unsupported audio format*");
    }

    [Fact]
    public async Task LoadAsync_rejects_audio_longer_than_configured_limit()
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

        await act.Should().ThrowAsync<AudioLoadingException>()
            .WithMessage("*maximum allowed duration*");
    }

    private static NAudioAnalysisAudioLoader CreateLoader(AudioLoaderOptions? options = null)
    {
        return new NAudioAnalysisAudioLoader(options);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "EternalLoopAnalysisEngineTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);
        return path;
    }
}
