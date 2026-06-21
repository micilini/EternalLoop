using System.Numerics;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;
using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisPreprocessor
{
    private readonly BeatThisPreprocessorOptions _options;
    private readonly int _hopSize;
    private readonly double[] _window;
    private readonly double[][] _melFilterBank;

    public BeatThisPreprocessor(BeatThisPreprocessorOptions? options = null)
    {
        _options = options ?? new BeatThisPreprocessorOptions();
        ValidateOptions(_options);

        _hopSize = Math.Max(1, (int)Math.Round(_options.SampleRate / _options.FrameRate));
        _window = WindowFunctions.Hann(_options.FrameSize);
        _melFilterBank = MelScale.CreateFilterBank(
            _options.SampleRate,
            _options.FrameSize,
            _options.MelBins,
            _options.MinFrequency,
            _options.MaxFrequency);
    }

    public BeatThisInputTensor Preprocess(LoadedAudio audio)
    {
        var chunks = PreprocessChunks(audio);

        if (chunks.Count == 0)
        {
            throw new InvalidDataException("Beat This preprocessor did not produce any chunks.");
        }

        return chunks[0];
    }

    public IReadOnlyList<BeatThisInputTensor> PreprocessChunks(LoadedAudio audio)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(audio.Samples);

        if (audio.SampleRate != _options.SampleRate)
        {
            throw new InvalidOperationException(
                $"Beat This preprocessor expected sample rate {_options.SampleRate} Hz, got {audio.SampleRate} Hz. "
                + "Resampling must happen before preprocessing.");
        }

        var totalFrameCount = CalculateFrameCount(audio.Samples.Length);

        if (totalFrameCount <= 0)
        {
            return [];
        }

        var chunks = new List<BeatThisInputTensor>();

        for (var startFrame = 0; startFrame < totalFrameCount; startFrame += _options.ChunkFrames)
        {
            var validFrameCount = Math.Min(_options.ChunkFrames, totalFrameCount - startFrame);
            var data = new float[_options.ChunkFrames * _options.MelBins];

            for (var localFrameIndex = 0; localFrameIndex < validFrameCount; localFrameIndex++)
            {
                var sourceFrameIndex = startFrame + localFrameIndex;

                WriteLogMelFrame(audio.Samples, sourceFrameIndex, localFrameIndex, data);
            }

            if (_options.Normalize && validFrameCount > 0)
            {
                NormalizeValidFrames(data, validFrameCount);
            }

            chunks.Add(new BeatThisInputTensor(
                data,
                [1, _options.ChunkFrames, _options.MelBins],
                validFrameCount,
                _options.SampleRate,
                _options.FrameRate,
                _options.ChunkFrames,
                _options.MelBins,
                _options.FrameSize,
                _hopSize,
                audio.DurationSeconds,
                startFrame,
                startFrame / _options.FrameRate));
        }

        return chunks;
    }

    public BeatThisSpectrogram PreprocessFullTrack(LoadedAudio audio)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(audio.Samples);

        if (audio.SampleRate != _options.SampleRate)
        {
            throw new InvalidOperationException(
                $"Beat This preprocessor expected sample rate {_options.SampleRate} Hz, got {audio.SampleRate} Hz. "
                + "Resampling must happen before preprocessing.");
        }

        var frameCount = CalculateFrameCount(audio.Samples.Length);

        if (frameCount <= 0)
        {
            throw new InvalidDataException("Beat This preprocessor did not produce any full-track frames.");
        }

        var data = new float[frameCount * _options.MelBins];

        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            WriteLogMelFrame(audio.Samples, frameIndex, frameIndex, data);
        }

        if (_options.Normalize)
        {
            NormalizeValidFrames(data, frameCount);
        }

        return new BeatThisSpectrogram(
            data,
            frameCount,
            _options.MelBins,
            _options.FrameRate,
            audio.DurationSeconds);
    }

    private void WriteLogMelFrame(
        float[] samples,
        int sourceFrameIndex,
        int destinationFrameIndex,
        float[] destination)
    {
        var buffer = new Complex[_options.FrameSize];
        var startSample = sourceFrameIndex * _hopSize;

        for (var offset = 0; offset < _options.FrameSize; offset++)
        {
            var sampleIndex = startSample + offset;
            var sample = sampleIndex < samples.Length ? samples[sampleIndex] : 0.0f;

            buffer[offset] = new Complex(sample * _window[offset], 0.0);
        }

        FftUtility.Forward(buffer);

        var spectrum = new double[(_options.FrameSize / 2) + 1];

        for (var bin = 0; bin < spectrum.Length; bin++)
        {
            spectrum[bin] = buffer[bin].Real * buffer[bin].Real
                + buffer[bin].Imaginary * buffer[bin].Imaginary;
        }

        for (var mel = 0; mel < _options.MelBins; mel++)
        {
            var energy = 0.0;
            var filter = _melFilterBank[mel];

            for (var bin = 0; bin < spectrum.Length && bin < filter.Length; bin++)
            {
                energy += spectrum[bin] * filter[bin];
            }

            var value = Math.Log(energy + _options.LogEpsilon);

            destination[(destinationFrameIndex * _options.MelBins) + mel] = (float)value;
        }
    }

    private void NormalizeValidFrames(float[] data, int validFrameCount)
    {
        var valueCount = validFrameCount * _options.MelBins;
        var mean = 0.0;

        for (var index = 0; index < valueCount; index++)
        {
            mean += data[index];
        }

        mean /= valueCount;

        var variance = 0.0;

        for (var index = 0; index < valueCount; index++)
        {
            var delta = data[index] - mean;

            variance += delta * delta;
        }

        variance /= valueCount;

        var stdDev = Math.Sqrt(Math.Max(variance, 1e-12));

        for (var index = 0; index < valueCount; index++)
        {
            data[index] = (float)((data[index] - mean) / stdDev);
        }
    }

    private int CalculateFrameCount(int sampleCount)
    {
        if (sampleCount <= 0)
        {
            return 0;
        }

        return 1 + ((sampleCount - 1) / _hopSize);
    }

    private static void ValidateOptions(BeatThisPreprocessorOptions options)
    {
        if (options.SampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.SampleRate));
        }

        if (options.FrameRate <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.FrameRate));
        }

        if (options.ChunkFrames <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.ChunkFrames));
        }

        if (options.MelBins <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MelBins));
        }

        if (options.FrameSize <= 0 || (options.FrameSize & (options.FrameSize - 1)) != 0)
        {
            throw new ArgumentException("FrameSize must be a positive power of two.", nameof(options.FrameSize));
        }

        if (options.LogEpsilon <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.LogEpsilon));
        }
    }
}
