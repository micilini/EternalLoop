using System.Numerics;

namespace EternalLoop.Core.Analysis;

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
        var bins = frameSize / 2 + 1;

        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            Array.Clear(fftBuffer);

            var sampleOffset = frameIndex * hopLength;

            for (var i = 0; i < frameSize; i++)
            {
                var sourceIndex = sampleOffset + i;
                var sample = sourceIndex < samples.Length ? samples[sourceIndex] : 0.0f;
                fftBuffer[i] = new Complex(sample * window[i], 0.0);
            }

            FftUtility.Forward(fftBuffer);

            var magnitudes = new float[bins];
            var power = new float[bins];

            for (var bin = 0; bin < bins; bin++)
            {
                var magnitude = fftBuffer[bin].Magnitude;
                magnitudes[bin] = (float)magnitude;
                power[bin] = (float)(magnitude * magnitude);
            }

            frames[frameIndex] = new StftFrame
            {
                Magnitudes = magnitudes,
                PowerSpectrum = power
            };
        }

        return frames;
    }
}
