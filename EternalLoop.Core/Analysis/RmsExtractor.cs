namespace EternalLoop.Core.Analysis;

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

        if (samples.Length < frameSize)
        {
            return [];
        }

        var frameCount = ((samples.Length - frameSize) / hopLength) + 1;
        var rms = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            var start = frame * hopLength;
            double sumSquares = 0.0;

            for (var i = 0; i < frameSize; i++)
            {
                var sample = samples[start + i];
                sumSquares += sample * sample;
            }

            rms[frame] = (float)Math.Sqrt(sumSquares / frameSize);
        }

        return rms;
    }
}
