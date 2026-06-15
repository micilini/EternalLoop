using EternalLoop.Playback.Audio;
using FluentAssertions;
using NAudio.Wave;

namespace EternalLoop.Playback.Tests.Audio;

public sealed class AudioLoaderTests
{
    [Fact]
    public async Task LoadAsyncShouldLoadWaveFileWithExpectedFormat()
    {
        using TempWaveFile waveFile = TempWaveFile.Create(sampleRate: 8000, channels: 2, frameCount: 16);
        var loader = new AudioLoader();

        LoadedAudio audio = await loader.LoadAsync(waveFile.Path);

        audio.SourcePath.Should().Be(Path.GetFullPath(waveFile.Path));
        audio.SampleRate.Should().Be(8000);
        audio.Channels.Should().Be(2);
        audio.Samples.Length.Should().BeGreaterThanOrEqualTo(32);
    }

    [Fact]
    public async Task LoadAsyncShouldPreserveSampleCountAndDuration()
    {
        using TempWaveFile waveFile = TempWaveFile.Create(sampleRate: 4000, channels: 1, frameCount: 20);
        var loader = new AudioLoader();

        LoadedAudio audio = await loader.LoadAsync(waveFile.Path);

        audio.TotalSampleFrames.Should().Be(20);
        audio.DurationSeconds.Should().BeApproximately(0.005, 0.000001);

        for (int index = 0; index < waveFile.Samples.Length; index++)
        {
            audio.Samples[index].Should().BeApproximately(waveFile.Samples[index], 0.000001f);
        }
    }

    [Fact]
    public void LoadAsyncShouldRejectEmptyPath()
    {
        var loader = new AudioLoader();

        Action act = () => loader.LoadAsync(" ");

        act.Should().Throw<AudioLoadException>()
            .WithMessage("Audio path cannot be empty.");
    }

    [Fact]
    public void LoadAsyncShouldRejectUnsupportedExtension()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "not audio");
        var loader = new AudioLoader();

        try
        {
            Action act = () => loader.LoadAsync(path);

            act.Should().Throw<AudioLoadException>()
                .WithMessage("Audio file extension is not supported. Choose an MP3, WAV, M4A or AAC file.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadAsyncShouldRejectOggExtension()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ogg");
        File.WriteAllText(path, "not audio");
        var loader = new AudioLoader();

        try
        {
            Action act = () => loader.LoadAsync(path);

            act.Should().Throw<AudioLoadException>()
                .WithMessage("Audio file extension is not supported. Choose an MP3, WAV, M4A or AAC file.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadAsyncShouldRejectFlacExtension()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.flac");
        File.WriteAllText(path, "not audio");
        var loader = new AudioLoader();

        try
        {
            Action act = () => loader.LoadAsync(path);

            act.Should().Throw<AudioLoadException>()
                .WithMessage("Audio file extension is not supported. Choose an MP3, WAV, M4A or AAC file.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadAsyncShouldAcceptSupportedRuntimeExtensionsByPolicy()
    {
        string[] extensions = [".mp3", ".wav", ".m4a", ".aac"];

        foreach (string extension in extensions)
        {
            SupportedAudioFormats.IsSupportedExtension(extension).Should().BeTrue();
            SupportedAudioFormats.IsSupportedExtension($"track{extension}").Should().BeTrue();
        }
    }

    [Fact]
    public void SupportedAudioFormatsShouldExposeOnlyRuntimeSupportedExtensions()
    {
        SupportedAudioFormats.Extensions.Should().BeEquivalentTo([".mp3", ".wav", ".m4a", ".aac"]);
        SupportedAudioFormats.Extensions.Should().NotContain(".ogg");
        SupportedAudioFormats.Extensions.Should().NotContain(".flac");
        SupportedAudioFormats.DialogFilter.Should().Be("Audio files|*.mp3;*.wav;*.m4a;*.aac|All files|*.*");
        SupportedAudioFormats.DisplayName.Should().Be("MP3, WAV, M4A or AAC");
    }

    [Fact]
    public void LoadAsyncShouldRejectMissingFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var loader = new AudioLoader();

        Action act = () => loader.LoadAsync(path);

        act.Should().Throw<AudioLoadException>()
            .WithMessage("Audio file does not exist:*");
    }

    [Fact]
    public async Task LoadAsyncShouldHonorCancellation()
    {
        using TempWaveFile waveFile = TempWaveFile.Create(sampleRate: 8000, channels: 1, frameCount: 16);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var loader = new AudioLoader();

        Func<Task> act = async () => await loader.LoadAsync(waveFile.Path, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadAsyncShouldRejectAudioLongerThanConfiguredLimit()
    {
        using TempWaveFile waveFile = TempWaveFile.Create(sampleRate: 1000, channels: 1, frameCount: 2000);
        var loader = new AudioLoader(new AudioLoadLimits
        {
            MaxDuration = TimeSpan.FromSeconds(1),
            MaxDecodedSamples = 10_000
        });

        Func<Task> act = async () => await loader.LoadAsync(waveFile.Path);

        await act.Should().ThrowAsync<AudioLoadException>()
            .WithMessage("Audio file is too long for loop mode. Maximum supported duration is*minutes.");
    }

    [Fact]
    public async Task LoadAsyncShouldRejectEstimatedDecodedSamplesAboveLimit()
    {
        using TempWaveFile waveFile = TempWaveFile.Create(sampleRate: 1000, channels: 1, frameCount: 100);
        var loader = new AudioLoader(new AudioLoadLimits
        {
            MaxDuration = TimeSpan.FromMinutes(1),
            MaxDecodedSamples = 50
        });

        Func<Task> act = async () => await loader.LoadAsync(waveFile.Path);

        await act.Should().ThrowAsync<AudioLoadException>()
            .WithMessage("Audio file is too large for loop mode.");
    }

    [Fact]
    public void LoadAsyncShouldRejectDecodedSamplesAboveLimitWhileReading()
    {
        string source = File.ReadAllText(FindRepositoryFile("src/EternalLoop.Playback/Audio/AudioLoader.cs"));

        source.Should().Contain("ValidateDecodedSampleCount(requiredCapacity, _limits);");
        source.Should().Contain("EnsureCapacity(ref samples, requiredCapacity, _limits);");
    }

    [Fact]
    public void LoadAsyncShouldWrapOutOfMemoryAsAudioLoadException()
    {
        string source = File.ReadAllText(FindRepositoryFile("src/EternalLoop.Playback/Audio/AudioLoader.cs"));

        source.Should().Contain("catch (OutOfMemoryException exception)");
        source.Should().Contain("throw new AudioLoadException(\"Audio file is too large to load into memory.\", exception);");
    }

    [Fact]
    public async Task LoadAsyncShouldStillLoadValidWaveUnderLimit()
    {
        using TempWaveFile waveFile = TempWaveFile.Create(sampleRate: 1000, channels: 2, frameCount: 100);
        var loader = new AudioLoader(new AudioLoadLimits
        {
            MaxDuration = TimeSpan.FromSeconds(10),
            MaxDecodedSamples = 500
        });

        LoadedAudio audio = await loader.LoadAsync(waveFile.Path);

        audio.TotalSampleFrames.Should().Be(100);
        audio.Channels.Should().Be(2);
        audio.SampleRate.Should().Be(1000);
    }

    private sealed class TempWaveFile : IDisposable
    {
        private TempWaveFile(string path, float[] samples)
        {
            Path = path;
            Samples = samples;
        }

        public string Path { get; }

        public float[] Samples { get; }

        public static TempWaveFile Create(int sampleRate, int channels, int frameCount)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
            float[] samples = new float[frameCount * channels];

            for (int index = 0; index < samples.Length; index++)
            {
                samples[index] = (index + 1) / (float)samples.Length;
            }

            WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            using (var writer = new WaveFileWriter(path, waveFormat))
            {
                writer.WriteSamples(samples, 0, samples.Length);
            }

            return new TempWaveFile(path, samples);
        }

        public void Dispose()
        {
            File.Delete(Path);
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
