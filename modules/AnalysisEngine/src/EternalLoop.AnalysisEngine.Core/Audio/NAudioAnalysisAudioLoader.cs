using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Runtime.InteropServices;

namespace EternalLoop.AnalysisEngine.Core.Audio;

public sealed class NAudioAnalysisAudioLoader : IAudioLoader
{
    private const string SupportedFormatList = "WAV, MP3, M4A, AAC and MP4 audio";

    private readonly AudioLoaderOptions _options;

    public NAudioAnalysisAudioLoader(AudioLoaderOptions? options = null)
    {
        _options = options ?? new AudioLoaderOptions();
    }

    public async Task<LoadedAudio> LoadAsync(
        string filePath,
        int targetSampleRate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new AudioLoadingException("Audio file path cannot be empty.");
        }

        if (targetSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetSampleRate),
                "Target sample rate must be greater than zero.");
        }

        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new AudioLoadingException($"Audio file not found: {fullPath}");
        }

        var detection = AudioFormatDetector.Detect(fullPath);

        if (!detection.IsSupported)
        {
            throw new AudioLoadingException(
                $"Unsupported audio format: {detection.Extension}. Supported formats: {SupportedFormatList}.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fileHash = await AudioHashCalculator
            .Sha256Async(fullPath, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            using var reader = CreateReader(fullPath, detection.Format);

            if (reader.TotalTime > _options.MaxDuration)
            {
                throw new AudioLoadingException(
                    $"Audio exceeds the maximum allowed duration of {_options.MaxDuration.TotalMinutes:0.#} minutes.");
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
                fileHash,
                fullPath,
                Path.GetFileName(fullPath));
        }
        catch (AudioLoadingException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException exception)
        {
            throw new AudioLoadingException($"Audio file not found: {fullPath}", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new AudioLoadingException($"Access denied while reading audio file: {fullPath}", exception);
        }
        catch (COMException exception)
        {
            throw new AudioLoadingException("Unsupported audio format or corrupted audio file.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new AudioLoadingException(
                "Could not decode audio file. The file may be unsupported or corrupted.",
                exception);
        }
        catch (IOException exception)
        {
            throw new AudioLoadingException(
                "Could not read audio file. The file may be locked or corrupted.",
                exception);
        }
        catch (OutOfMemoryException exception)
        {
            throw new AudioLoadingException("Audio file is too large to decode in memory.", exception);
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
