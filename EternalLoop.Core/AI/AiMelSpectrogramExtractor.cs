using EternalLoop.Contracts.Options;
using EternalLoop.Core.Analysis;

namespace EternalLoop.Core.AI;

public sealed class AiMelSpectrogramExtractor
{
    private readonly AiMelFilterBank _filterBank;

    public AiMelSpectrogramExtractor(AiMelFilterBank filterBank)
    {
        _filterBank = filterBank ?? throw new ArgumentNullException(nameof(filterBank));
    }

    public float[][] Extract(
        float[] samples,
        int sampleRate,
        int melBands,
        int fftSize,
        int hopLength)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (melBands <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(melBands));
        }

        if (fftSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fftSize));
        }

        if (hopLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopLength));
        }

        if (samples.Length == 0)
        {
            return [];
        }

        var sanitizedSamples = samples.Select(Sanitize).ToArray();
        var stftFrames = StftAnalyzer.Analyze(sanitizedSamples, fftSize, hopLength);
        var filters = _filterBank.Create(sampleRate, fftSize, melBands, AiPreprocessingDefaultValues.MinFrequencyHertz);
        var spectrogram = new float[stftFrames.Length][];
        var sum = 0.0;
        var valueCount = stftFrames.Length * melBands;

        for (var frameIndex = 0; frameIndex < stftFrames.Length; frameIndex++)
        {
            var frameValues = new float[melBands];
            var powerSpectrum = stftFrames[frameIndex].PowerSpectrum;

            for (var melBandIndex = 0; melBandIndex < melBands; melBandIndex++)
            {
                var filter = filters[melBandIndex];
                var energy = 0.0;

                for (var binIndex = 0; binIndex < powerSpectrum.Length; binIndex++)
                {
                    energy += powerSpectrum[binIndex] * filter[binIndex];
                }

                var value = Math.Log(Math.Max(energy, AiPreprocessingDefaultValues.LogFloor));
                frameValues[melBandIndex] = (float)value;
                sum += value;
            }

            spectrogram[frameIndex] = frameValues;
        }

        var mean = sum / valueCount;
        var varianceSum = 0.0;

        for (var frameIndex = 0; frameIndex < spectrogram.Length; frameIndex++)
        {
            for (var melBandIndex = 0; melBandIndex < melBands; melBandIndex++)
            {
                var centered = spectrogram[frameIndex][melBandIndex] - mean;
                varianceSum += centered * centered;
            }
        }

        var standardDeviation = Math.Sqrt(varianceSum / valueCount);

        if (standardDeviation < AiPreprocessingDefaultValues.NormalizationEpsilon)
        {
            return spectrogram
                .Select(frame => frame.Select(value => Sanitize((float)(value - mean))).ToArray())
                .ToArray();
        }

        return spectrogram
            .Select(frame => frame.Select(value => Sanitize((float)((value - mean) / standardDeviation))).ToArray())
            .ToArray();
    }

    private static float Sanitize(float value)
    {
        return float.IsFinite(value) ? value : 0.0f;
    }
}
