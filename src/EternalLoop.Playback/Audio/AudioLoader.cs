using NAudio.Wave;

namespace EternalLoop.Playback.Audio;

public sealed class AudioLoader : IAudioLoader
{
    private const int MinimumBufferSamples = 4096;
    private readonly AudioLoadLimits _limits;

    public AudioLoader()
        : this(AudioLoadLimits.Default)
    {
    }

    public AudioLoader(AudioLoadLimits limits)
    {
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        ValidateLimits(_limits);
    }

    public Task<LoadedAudio> LoadAsync(string audioPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioPath))
        {
            throw new AudioLoadException("Audio path cannot be empty.");
        }

        string fullPath = Path.GetFullPath(audioPath);

        if (!SupportedAudioFormats.IsSupportedExtension(fullPath))
        {
            throw new AudioLoadException($"Audio file extension is not supported. Choose an {SupportedAudioFormats.DisplayName} file.");
        }

        if (!File.Exists(fullPath))
        {
            throw new AudioLoadException($"Audio file does not exist: {fullPath}");
        }

        return Task.Run(() => LoadCore(fullPath, cancellationToken), cancellationToken);
    }

    private LoadedAudio LoadCore(string fullPath, CancellationToken cancellationToken)
    {
        try
        {
            using AudioFileReader reader = new(fullPath);

            int sampleRate = reader.WaveFormat.SampleRate;
            int channels = reader.WaveFormat.Channels;

            if (sampleRate <= 0 || channels <= 0)
            {
                throw new AudioLoadException("Audio file has invalid wave format.");
            }

            ValidateDuration(reader.TotalTime, _limits);

            int sampleCapacity = EstimateOutputSampleCapacity(reader, sampleRate, channels, _limits);
            float[] samples = AllocateSampleBuffer(sampleCapacity);
            float[] buffer = new float[CalculateReadBufferSize(sampleRate, channels)];
            int sampleCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read = reader.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                int requiredCapacity = checked(sampleCount + read);
                ValidateDecodedSampleCount(requiredCapacity, _limits);
                EnsureCapacity(ref samples, requiredCapacity, _limits);
                Array.Copy(buffer, 0, samples, sampleCount, read);
                sampleCount += read;
            }

            if (sampleCount == 0)
            {
                throw new AudioLoadException("Audio file has no samples.");
            }

            int totalSampleFrames = sampleCount / channels;
            double durationSeconds = totalSampleFrames / (double)sampleRate;

            return new LoadedAudio
            {
                SourcePath = fullPath,
                Samples = samples,
                SampleRate = sampleRate,
                Channels = channels,
                DurationSeconds = durationSeconds,
                TotalSampleFrames = totalSampleFrames
            };
        }
        catch (AudioLoadException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AudioLoadException($"Could not load audio file: {fullPath}", exception);
        }
    }

    private static int EstimateOutputSampleCapacity(AudioFileReader reader, int sampleRate, int channels, AudioLoadLimits limits)
    {
        long capacity = 0;

        if (reader.TotalTime.TotalSeconds > 0 && double.IsFinite(reader.TotalTime.TotalSeconds))
        {
            double estimatedSamples = Math.Ceiling(reader.TotalTime.TotalSeconds * sampleRate * channels);

            if (estimatedSamples > int.MaxValue)
            {
                throw new AudioLoadException("Audio file is too large to load into memory.");
            }

            ValidateDecodedSampleCount((long)estimatedSamples, limits);
            capacity = Math.Max(capacity, (long)estimatedSamples);
        }

        if (reader.Length > 0)
        {
            long lengthSamples = reader.Length / sizeof(float);
            ValidateDecodedSampleCount(lengthSamples, limits);
            capacity = Math.Max(capacity, lengthSamples);
        }

        if (capacity > int.MaxValue)
        {
            throw new AudioLoadException("Audio file is too large to load into memory.");
        }

        ValidateDecodedSampleCount(capacity, limits);
        long fallbackCapacity = Math.Min(CalculateReadBufferSize(sampleRate, channels), limits.MaxDecodedSamples);
        long finalCapacity = Math.Max(capacity, fallbackCapacity);
        ValidateDecodedSampleCount(finalCapacity, limits);
        return (int)finalCapacity;
    }

    private static int CalculateReadBufferSize(int sampleRate, int channels)
    {
        try
        {
            return Math.Max(checked(sampleRate * channels), MinimumBufferSamples);
        }
        catch (OverflowException exception)
        {
            throw new AudioLoadException("Audio file has invalid wave format.", exception);
        }
    }

    private static float[] AllocateSampleBuffer(int capacity)
    {
        try
        {
            return new float[capacity];
        }
        catch (OutOfMemoryException exception)
        {
            throw new AudioLoadException("Audio file is too large to load into memory.", exception);
        }
    }

    private static void EnsureCapacity(ref float[] samples, int requiredCapacity, AudioLoadLimits limits)
    {
        ValidateDecodedSampleCount(requiredCapacity, limits);

        if (requiredCapacity <= samples.Length)
        {
            return;
        }

        int newCapacity = samples.Length == 0 ? MinimumBufferSamples : samples.Length;

        while (newCapacity < requiredCapacity)
        {
            long grownCapacity = Math.Max((long)newCapacity * 2, requiredCapacity);

            if (grownCapacity > int.MaxValue)
            {
                throw new AudioLoadException("Audio file is too large to load into memory.");
            }

            ValidateDecodedSampleCount(grownCapacity, limits);
            newCapacity = (int)grownCapacity;
        }

        try
        {
            Array.Resize(ref samples, newCapacity);
        }
        catch (OutOfMemoryException exception)
        {
            throw new AudioLoadException("Audio file is too large to load into memory.", exception);
        }
    }

    private static void ValidateDuration(TimeSpan duration, AudioLoadLimits limits)
    {
        if (duration > limits.MaxDuration)
        {
            throw new AudioLoadException(
                $"Audio file is too long for loop mode. Maximum supported duration is {limits.MaxDuration.TotalMinutes:0.#} minutes.");
        }
    }

    private static void ValidateDecodedSampleCount(long decodedSamples, AudioLoadLimits limits)
    {
        if (decodedSamples > limits.MaxDecodedSamples)
        {
            throw new AudioLoadException("Audio file is too large for loop mode.");
        }
    }

    private static void ValidateLimits(AudioLoadLimits limits)
    {
        if (limits.MaxDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(limits), "Maximum audio duration must be greater than zero.");
        }

        if (limits.MaxDecodedSamples <= 0 || limits.MaxDecodedSamples > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(limits), "Maximum decoded samples must be between 1 and int.MaxValue.");
        }
    }
}
