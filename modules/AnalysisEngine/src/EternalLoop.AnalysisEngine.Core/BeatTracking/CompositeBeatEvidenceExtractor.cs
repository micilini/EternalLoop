using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public static class CompositeBeatEvidenceExtractor
{
    public static CompositeBeatEvidence Extract(FeatureMatrix features, BeatTrackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(options);

        var length = new[]
        {
            features.OnsetEnvelope.Length,
            features.SpectralFlux.Length,
            features.Rms.Length,
            features.Mfcc.Length,
            features.Chroma.Length,
            features.HpssApplied ? features.PercussiveOnsetEnvelope.Length : features.OnsetEnvelope.Length
        }.Where(value => value > 0).DefaultIfEmpty(0).Min();

        if (length == 0)
        {
            return new CompositeBeatEvidence([], [], BuildWeights(options));
        }

        var onset = Normalize(features.OnsetEnvelope.Length >= length ? features.OnsetEnvelope.Take(length).Select(x => (double)x).ToArray() : features.SpectralFlux.Take(length).Select(x => (double)x).ToArray());
        var flux = Normalize(features.SpectralFlux.Take(length).Select(x => (double)x).ToArray());
        var rmsDelta = Normalize(Delta(features.Rms.Take(length).Select(x => (double)x).ToArray()));
        var mfccDelta = Normalize(VectorDelta(features.Mfcc.Take(length).ToArray()));
        var chromaDelta = Normalize(VectorDelta(features.Chroma.Take(length).ToArray()));
        var novelty = Normalize(Combine(mfccDelta, chromaDelta, rmsDelta));
        var percussiveOnset = Normalize(features.PercussiveOnsetEnvelope.Length >= length
            ? features.PercussiveOnsetEnvelope.Take(length).Select(x => (double)x).ToArray()
            : new double[length]);
        var percussiveFlux = Normalize(features.PercussiveSpectralFlux.Length >= length
            ? features.PercussiveSpectralFlux.Take(length).Select(x => (double)x).ToArray()
            : new double[length]);
        var percussiveRmsDelta = Normalize(Delta(features.PercussiveRms.Length >= length
            ? features.PercussiveRms.Take(length).Select(x => (double)x).ToArray()
            : new double[length]));
        var harmonicOnset = Normalize(features.HarmonicOnsetEnvelope.Length >= length
            ? features.HarmonicOnsetEnvelope.Take(length).Select(x => (double)x).ToArray()
            : new double[length]);

        var fusionOnset = options.UseHpss && features.HpssApplied
            ? Normalize(WeightedCombine(
                (onset, options.FullMixOnsetWeight),
                (percussiveOnset, options.PercussiveOnsetWeight),
                (harmonicOnset, options.HarmonicOnsetWeight)))
            : onset;
        var fusionFlux = options.UseHpss && features.HpssApplied
            ? Normalize(WeightedCombine(
                (flux, options.FullMixOnsetWeight),
                (percussiveFlux, options.PercussiveOnsetWeight),
                (harmonicOnset, options.HarmonicOnsetWeight)))
            : flux;

        var low = Smooth(options.UseHpss && features.HpssApplied ? Combine(fusionFlux, percussiveFlux) : flux, 3);
        var mid = Smooth(Combine(fusionFlux, mfccDelta), 5);
        var high = Normalize(fusionOnset.Zip(fusionFlux, (a, b) => Math.Max(0.0, a - b * 0.35)).ToArray());
        var weights = BuildWeights(options);
        var composite = new float[length];
        var frames = new BeatEvidenceFrame[length];

        for (var i = 0; i < length; i++)
        {
            var value =
                weights["logMelOnset"] * fusionOnset[i]
                + weights["lowBandOnset"] * low[i]
                + weights["midBandOnset"] * mid[i]
                + weights["highBandOnset"] * high[i]
                + weights["rmsDelta"] * (options.UseHpss && features.HpssApplied ? Math.Max(rmsDelta[i], percussiveRmsDelta[i]) : rmsDelta[i])
                + weights["mfccDelta"] * mfccDelta[i]
                + weights["chromaDelta"] * chromaDelta[i]
                + weights["novelty"] * novelty[i];
            composite[i] = (float)Math.Clamp(value, 0.0, 1.0);
            frames[i] = new BeatEvidenceFrame(
                fusionOnset[i],
                fusionOnset[i],
                low[i],
                mid[i],
                high[i],
                rmsDelta[i],
                mfccDelta[i],
                chromaDelta[i],
                novelty[i],
                percussiveOnset[i],
                percussiveFlux[i],
                percussiveRmsDelta[i],
                percussiveFlux[i],
                harmonicOnset[i],
                composite[i]);
        }

        return new CompositeBeatEvidence(frames, composite, weights);
    }

    private static IReadOnlyDictionary<string, double> BuildWeights(BeatTrackingOptions options)
    {
        return new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["logMelOnset"] = options.BeatEvidenceLogMelOnsetWeight,
            ["lowBandOnset"] = options.BeatEvidenceLowBandOnsetWeight,
            ["midBandOnset"] = options.BeatEvidenceMidBandOnsetWeight,
            ["highBandOnset"] = options.BeatEvidenceHighBandOnsetWeight,
            ["rmsDelta"] = options.BeatEvidenceRmsDeltaWeight,
            ["mfccDelta"] = options.BeatEvidenceMfccDeltaWeight,
            ["chromaDelta"] = options.BeatEvidenceChromaDeltaWeight,
            ["novelty"] = options.BeatEvidenceNoveltyWeight
        };
    }

    private static double[] VectorDelta(float[][] values)
    {
        if (values.Length == 0)
        {
            return [];
        }

        var result = new double[values.Length];
        for (var i = 1; i < values.Length; i++)
        {
            var length = Math.Min(values[i].Length, values[i - 1].Length);
            var sum = 0.0;
            for (var j = 0; j < length; j++)
            {
                sum += Math.Pow(values[i][j] - values[i - 1][j], 2.0);
            }

            result[i] = Math.Sqrt(sum);
        }

        return result;
    }

    private static double[] Delta(double[] values)
    {
        var result = new double[values.Length];
        for (var i = 1; i < values.Length; i++)
        {
            result[i] = Math.Max(0.0, values[i] - values[i - 1]);
        }

        return result;
    }

    private static double[] Combine(params double[][] channels)
    {
        var length = channels.Where(x => x.Length > 0).DefaultIfEmpty([]).Min(x => x.Length);
        if (length == 0)
        {
            return [];
        }

        var result = new double[length];
        foreach (var channel in channels)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] += channel[i] / channels.Length;
            }
        }

        return result;
    }

    private static double[] WeightedCombine(params (double[] Values, double Weight)[] channels)
    {
        var length = channels
            .Where(channel => channel.Values.Length > 0 && channel.Weight > 0.0)
            .DefaultIfEmpty(([], 0.0))
            .Min(channel => channel.Values.Length);
        if (length == 0)
        {
            return [];
        }

        var totalWeight = channels.Where(channel => channel.Weight > 0.0).Sum(channel => channel.Weight);
        if (totalWeight <= 0.0)
        {
            return new double[length];
        }

        var result = new double[length];
        foreach (var channel in channels.Where(channel => channel.Weight > 0.0))
        {
            for (var i = 0; i < length; i++)
            {
                result[i] += channel.Values[i] * channel.Weight / totalWeight;
            }
        }

        return result;
    }

    private static double[] Smooth(double[] values, int radius)
    {
        var result = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var start = Math.Max(0, i - radius);
            var end = Math.Min(values.Length - 1, i + radius);
            result[i] = values.Skip(start).Take(end - start + 1).Average();
        }

        return Normalize(result);
    }

    private static double[] Normalize(double[] values)
    {
        if (values.Length == 0)
        {
            return [];
        }

        var sorted = values.Where(double.IsFinite).Order().ToArray();
        var p95 = sorted.Length == 0 ? 1.0 : sorted[(int)Math.Clamp(Math.Round((sorted.Length - 1) * 0.95), 0, sorted.Length - 1)];
        var scale = Math.Max(1e-9, p95);
        return values.Select(value => Math.Clamp(value / scale, 0.0, 1.0)).ToArray();
    }
}
