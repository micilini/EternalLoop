using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.Features;

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

        for (var frameIndex = 0; frameIndex < mfcc.Length; frameIndex++)
        {
            combined[frameIndex] = new float[mfcc[frameIndex].Length + deltas[frameIndex].Length];
            Array.Copy(mfcc[frameIndex], 0, combined[frameIndex], 0, mfcc[frameIndex].Length);
            Array.Copy(deltas[frameIndex], 0, combined[frameIndex], mfcc[frameIndex].Length, deltas[frameIndex].Length);
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
        var valueCount = values.Length;

        for (var coefficient = 0; coefficient < coefficientCount; coefficient++)
        {
            double sum = 0.0;

            for (var index = 0; index < valueCount; index++)
            {
                sum += values[index] * Math.Cos(Math.PI * coefficient * (index + 0.5) / valueCount);
            }

            result[coefficient] = (float)sum;
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

        for (var frameIndex = 0; frameIndex < vectors.Length; frameIndex++)
        {
            deltas[frameIndex] = new float[dimension];

            var previous = vectors[Math.Max(0, frameIndex - 1)];
            var next = vectors[Math.Min(vectors.Length - 1, frameIndex + 1)];

            for (var dimensionIndex = 0; dimensionIndex < dimension; dimensionIndex++)
            {
                deltas[frameIndex][dimensionIndex] = (next[dimensionIndex] - previous[dimensionIndex]) * 0.5f;
            }
        }

        return deltas;
    }
}
