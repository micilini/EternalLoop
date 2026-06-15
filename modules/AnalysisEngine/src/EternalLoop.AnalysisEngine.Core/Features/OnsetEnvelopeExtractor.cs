namespace EternalLoop.AnalysisEngine.Core.Features;

internal static class OnsetEnvelopeExtractor
{
    private const int MelBandCount = 128;
    private const double MinimumPower = 1e-10;
    private const double TopDb = 80.0;

    public static float[] Compute(StftFrame[] frames, int sampleRate, int frameSize)
    {
        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Length == 0)
        {
            return [];
        }

        var filterBank = MelScale.CreateFilterBank(
            sampleRate,
            frameSize,
            MelBandCount,
            minFrequency: 20.0,
            maxFrequency: sampleRate / 2.0);
        var melDb = new double[frames.Length][];
        var globalMax = double.NegativeInfinity;

        for (var frame = 0; frame < frames.Length; frame++)
        {
            melDb[frame] = new double[MelBandCount];

            for (var band = 0; band < MelBandCount; band++)
            {
                var sum = 0.0;
                var weights = filterBank[band];
                var length = Math.Min(weights.Length, frames[frame].PowerSpectrum.Length);

                for (var bin = 0; bin < length; bin++)
                {
                    sum += frames[frame].PowerSpectrum[bin] * weights[bin];
                }

                var db = 10.0 * Math.Log10(Math.Max(MinimumPower, sum));
                melDb[frame][band] = db;
                globalMax = Math.Max(globalMax, db);
            }
        }

        var floor = globalMax - TopDb;
        for (var frame = 0; frame < melDb.Length; frame++)
        {
            for (var band = 0; band < MelBandCount; band++)
            {
                melDb[frame][band] = Math.Max(floor, melDb[frame][band]);
            }
        }

        var envelope = new float[frames.Length];
        for (var frame = 1; frame < frames.Length; frame++)
        {
            var sum = 0.0;

            for (var band = 0; band < MelBandCount; band++)
            {
                sum += Math.Max(0.0, melDb[frame][band] - melDb[frame - 1][band]);
            }

            envelope[frame] = (float)(sum / MelBandCount);
        }

        return Normalize(envelope);
    }

    private static float[] Normalize(float[] values)
    {
        var max = values.Length == 0 ? 0.0f : values.Max();
        if (max <= 0.0f || !float.IsFinite(max))
        {
            return values;
        }

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = Math.Clamp(values[index] / max, 0.0f, 1.0f);
        }

        return values;
    }
}
