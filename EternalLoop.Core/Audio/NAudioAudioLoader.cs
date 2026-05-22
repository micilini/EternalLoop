using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Core.Hashing;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Runtime.InteropServices;

namespace EternalLoop.Core.Audio;

public sealed class NAudioAudioLoader : IAudioLoader
{
    private readonly AudioLoaderOptions _options;

    public NAudioAudioLoader(IOptions<AudioLoaderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public async Task<LoadedAudio> LoadAsync(
        string filePath,
        int targetSampleRate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new AudioLoadException("Audio file path cannot be empty.");
        }

        if (targetSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSampleRate), "Target sample rate must be greater than zero.");
        }

        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new AudioLoadException($"Audio file not found: {fullPath}");
        }

        var detection = AudioFormatDetector.Detect(fullPath);
        if (!detection.IsSupported)
        {
            throw new AudioLoadException($"Unsupported audio format: {detection.Extension}. Supported formats: WAV, MP3, FLAC, M4A and AAC.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fileHash = await FileHasher.Sha256Async(fullPath, cancellationToken).ConfigureAwait(false);

        try
        {
            using var reader = CreateReader(fullPath, detection.Format);

            if (reader.TotalTime > _options.MaxDuration)
            {
                throw new AudioLoadException($"Audio exceeds the maximum allowed duration of {_options.MaxDuration.TotalMinutes:0.#} minutes.");
            }

            var sourceProvider = reader.ToSampleProvider();
            var monoProvider = AudioConversion.ToMono(sourceProvider);

            ISampleProvider finalProvider = monoProvider.WaveFormat.SampleRate == targetSampleRate
                ? monoProvider
                : new WdlResamplingSampleProvider(monoProvider, targetSampleRate);

            var samples = AudioConversion.ReadAllSamples(
                finalProvider,
                cancellationToken,
                _options.ReadBufferSize,
                _options.MaxDuration);

            var durationSeconds = samples.Length / (double)targetSampleRate;

            return new LoadedAudio(
                samples,
                targetSampleRate,
                durationSeconds,
                fileHash);
        }
        catch (AudioLoadException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException ex)
        {
            throw new AudioLoadException($"Audio file not found: {fullPath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AudioLoadException($"Access denied while reading audio file: {fullPath}", ex);
        }
        catch (COMException ex)
        {
            throw new AudioLoadException("Unsupported audio format or corrupted audio file.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new AudioLoadException("Could not decode audio file. The file may be unsupported or corrupted.", ex);
        }
        catch (IOException ex)
        {
            throw new AudioLoadException("Could not read audio file. The file may be locked or corrupted.", ex);
        }
        catch (OutOfMemoryException ex)
        {
            throw new AudioLoadException("Audio file is too large to decode in memory.", ex);
        }
    }

    private static WaveStream CreateReader(string filePath, AudioFileFormat format)
    {
        if (format == AudioFileFormat.Wav)
        {
            return new WaveFileReader(filePath);
        }

        MediaFoundationInitializer.EnsureStarted();
        return new MediaFoundationReader(filePath);
    }
}
