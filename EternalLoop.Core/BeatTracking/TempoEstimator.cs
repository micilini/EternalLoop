namespace EternalLoop.Core.BeatTracking;

public static class TempoEstimator
{
    private const double FallbackBpm = 120.0;

    public static double EstimateBpm(
        float[] onsetDetectionFunction,
        int hopLengthSamples,
        int sampleRate,
        double minBpm,
        double maxBpm)
    {
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);

        if (hopLengthSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hopLengthSamples), "Hop length must be greater than zero.");
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
        }

        if (minBpm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minBpm), "Minimum BPM must be greater than zero.");
        }

        if (maxBpm <= minBpm)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBpm), "Maximum BPM must be greater than minimum BPM.");
        }

        if (onsetDetectionFunction.Length < 2 || onsetDetectionFunction.Max() <= 1e-9f)
        {
            return ClampBpm(FallbackBpm, minBpm, maxBpm);
        }

        var framesPerSecond = sampleRate / (double)hopLengthSamples;
        var minLag = Math.Max(1, (int)Math.Floor(framesPerSecond * 60.0 / maxBpm));
        var maxLag = Math.Max(minLag + 1, (int)Math.Ceiling(framesPerSecond * 60.0 / minBpm));

        maxLag = Math.Min(maxLag, onsetDetectionFunction.Length - 1);

        if (maxLag < minLag)
        {
            return ClampBpm(FallbackBpm, minBpm, maxBpm);
        }

        var bestLag = minLag;
        var bestScore = double.NegativeInfinity;
        var scoresByLag = new Dictionary<int, double>();

        for (var lag = minLag; lag <= maxLag; lag++)
        {
            double sum = 0.0;

            for (var i = 0; i < onsetDetectionFunction.Length - lag; i++)
            {
                sum += onsetDetectionFunction[i] * onsetDetectionFunction[i + lag];
            }

            var bpm = 60.0 * framesPerSecond / lag;
            var perceptualWeight = Math.Exp(-0.5 * Math.Pow(Math.Log2(bpm / 120.0) / 1.4, 2.0));
            var score = (sum / Math.Max(1, onsetDetectionFunction.Length - lag)) * perceptualWeight;
            scoresByLag[lag] = score;

            if (score > bestScore)
            {
                bestScore = score;
                bestLag = lag;
            }
        }

        bestLag = PreferSpecificTempoLag(bestLag, bestScore, scoresByLag, minLag, maxLag);

        var periodFrames = RefinePeriodFromPeaks(onsetDetectionFunction, bestLag) ?? bestLag;

        return ClampBpm(60.0 * framesPerSecond / periodFrames, minBpm, maxBpm);
    }

    private static int PreferSpecificTempoLag(
        int bestLag,
        double bestScore,
        IReadOnlyDictionary<int, double> scoresByLag,
        int minLag,
        int maxLag)
    {
        var selectedLag = bestLag;
        var selectedScore = bestScore;

        while (selectedLag / 2 >= minLag)
        {
            var halfLag = selectedLag / 2;

            if (halfLag < minLag || halfLag > maxLag || !scoresByLag.TryGetValue(halfLag, out var halfScore))
            {
                break;
            }

            if (halfScore < selectedScore * 0.35)
            {
                break;
            }

            selectedLag = halfLag;
            selectedScore = halfScore;
        }

        return selectedLag;
    }

    private static double? RefinePeriodFromPeaks(float[] odf, int referenceLag)
    {
        var max = odf.Max();
        if (max <= 1e-9f)
        {
            return null;
        }

        var threshold = max * 0.5f;
        var peaks = new List<int>();

        for (var i = 0; i < odf.Length; i++)
        {
            var left = i == 0 ? float.MinValue : odf[i - 1];
            var right = i == odf.Length - 1 ? float.MinValue : odf[i + 1];

            if (odf[i] >= threshold && odf[i] >= left && odf[i] > right)
            {
                peaks.Add(i);
            }
        }

        if (peaks.Count < 3)
        {
            return null;
        }

        var intervals = new List<int>();
        for (var i = 1; i < peaks.Count; i++)
        {
            var interval = peaks[i] - peaks[i - 1];

            if (interval >= referenceLag * 0.5 && interval <= referenceLag * 1.5)
            {
                intervals.Add(interval);
            }
        }

        if (intervals.Count == 0)
        {
            return null;
        }

        return intervals.Average();
    }

    private static double ClampBpm(double bpm, double minBpm, double maxBpm)
    {
        return Math.Clamp(bpm, minBpm, maxBpm);
    }
}
