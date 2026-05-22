using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.Analysis;

internal static class ClassicMfccExtractor
{
    private const double Epsilon = 1e-10;

    public static float[][] Compute(
        StftFrame[] frames,
        int sampleRate,
        int frameSize,
        FeatureExtractionOptions options)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(options);

        var filterBank = MelScale.CreateFilterBank(
            sampleRate,
            frameSize,
            options.FilterBankSize);

        var mfcc = new float[frames.Length][];

        for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
        {
            var logEnergies = ApplyFilterBank(frames[frameIndex].PowerSpectrum, filterBank);
            mfcc[frameIndex] = Dct(logEnergies, options.MfccCount);
        }

        if (!options.ComputeDeltas)
        {
            return mfcc;
        }

        var deltas = ComputeDeltas(mfcc);
        var combined = new float[mfcc.Length][];

        for (var i = 0; i < mfcc.Length; i++)
        {
            combined[i] = new float[mfcc[i].Length + deltas[i].Length];
            Array.Copy(mfcc[i], 0, combined[i], 0, mfcc[i].Length);
            Array.Copy(deltas[i], 0, combined[i], mfcc[i].Length, deltas[i].Length);
        }

        return combined;
    }

    private static double[] ApplyFilterBank(float[] powerSpectrum, double[][] filterBank)
    {
        var energies = new double[filterBank.Length];

        for (var filter = 0; filter < filterBank.Length; filter++)
        {
            double sum = 0.0;
            var weights = filterBank[filter];

            var length = Math.Min(powerSpectrum.Length, weights.Length);

            for (var bin = 0; bin < length; bin++)
            {
                sum += powerSpectrum[bin] * weights[bin];
            }

            energies[filter] = Math.Log(Math.Max(sum, Epsilon));
        }

        return energies;
    }

    private static float[] Dct(double[] values, int coefficientCount)
    {
        var result = new float[coefficientCount];
        var n = values.Length;

        for (var k = 0; k < coefficientCount; k++)
        {
            double sum = 0.0;

            for (var i = 0; i < n; i++)
            {
                sum += values[i] * Math.Cos(Math.PI * k * (i + 0.5) / n);
            }

            result[k] = (float)sum;
        }

        return result;
    }

    private static float[][] ComputeDeltas(float[][] vectors)
    {
        var deltas = new float[vectors.Length][];

        if (vectors.Length == 0)
        {
            return deltas;
        }

        var dimension = vectors[0].Length;

        for (var i = 0; i < vectors.Length; i++)
        {
            deltas[i] = new float[dimension];

            var previous = vectors[Math.Max(0, i - 1)];
            var next = vectors[Math.Min(vectors.Length - 1, i + 1)];

            for (var d = 0; d < dimension; d++)
            {
                deltas[i][d] = (next[d] - previous[d]) * 0.5f;
            }
        }

        return deltas;
    }
}
