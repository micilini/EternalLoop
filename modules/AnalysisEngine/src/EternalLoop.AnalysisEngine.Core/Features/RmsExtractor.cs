namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class RmsExtractor
{
    public static float[] Compute(float[] samples, int frameSize, int hopLength)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (frameSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameSize), "Frame size must be greater than zero.");
        }

        if (hopLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopLength), "Hop length must be greater than zero.");
        }

        if (samples.Length == 0)
        {
            return [];
        }

        var frameCount = samples.Length <= frameSize
            ? 1
            : 1 + (int)Math.Ceiling((samples.Length - frameSize) / (double)hopLength);

        var rms = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            var start = frame * hopLength;
            double sumSquares = 0.0;

            for (var index = 0; index < frameSize; index++)
            {
                var sourceIndex = start + index;
                var sample = sourceIndex < samples.Length ? samples[sourceIndex] : 0.0f;
                sumSquares += sample * sample;
            }

            rms[frame] = (float)Math.Sqrt(sumSquares / frameSize);
        }

        return rms;
    }
}
