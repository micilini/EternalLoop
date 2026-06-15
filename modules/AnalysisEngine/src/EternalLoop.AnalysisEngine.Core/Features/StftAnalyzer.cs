using System.Numerics;

namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class StftAnalyzer
{
    public static StftFrame[] Analyze(float[] samples, int frameSize, int hopLength)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Length == 0)
        {
            return [];
        }

        if (frameSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameSize));
        }

        if (hopLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopLength));
        }

        var frameCount = samples.Length <= frameSize
            ? 1
            : 1 + (int)Math.Ceiling((samples.Length - frameSize) / (double)hopLength);

        var window = WindowFunctions.Hann(frameSize);
        var frames = new StftFrame[frameCount];
        var fftBuffer = new Complex[frameSize];
        var binCount = (frameSize / 2) + 1;

        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            Array.Clear(fftBuffer);

            var sampleOffset = frameIndex * hopLength;

            for (var sampleIndex = 0; sampleIndex < frameSize; sampleIndex++)
            {
                var sourceIndex = sampleOffset + sampleIndex;
                var sample = sourceIndex < samples.Length ? samples[sourceIndex] : 0.0f;
                fftBuffer[sampleIndex] = new Complex(sample * window[sampleIndex], 0.0);
            }

            FftUtility.Forward(fftBuffer);

            var magnitudes = new float[binCount];
            var powerSpectrum = new float[binCount];

            for (var bin = 0; bin < binCount; bin++)
            {
                var magnitude = fftBuffer[bin].Magnitude;
                magnitudes[bin] = (float)magnitude;
                powerSpectrum[bin] = (float)(magnitude * magnitude);
            }

            frames[frameIndex] = new StftFrame
            {
                Magnitudes = magnitudes,
                PowerSpectrum = powerSpectrum
            };
        }

        return frames;
    }
}
