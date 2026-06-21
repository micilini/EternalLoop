namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisActivationSummary
{
    public int Count { get; init; }

    public double Min { get; init; }

    public double Max { get; init; }

    public double Mean { get; init; }

    public double P50 { get; init; }

    public double P75 { get; init; }

    public double P90 { get; init; }

    public double P95 { get; init; }

    public double P99 { get; init; }

    public int AboveThresholdCount { get; init; }

    public static BeatThisActivationSummary Empty { get; } = new();

    public static BeatThisActivationSummary From(
        IReadOnlyList<float> values,
        int count,
        double threshold)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (count <= 0)
        {
            return Empty;
        }

        if (values.Count < count)
        {
            throw new InvalidDataException("Activation summary source is smaller than requested count.");
        }

        var copy = new double[count];
        var sum = 0.0;
        var aboveThreshold = 0;

        for (var index = 0; index < count; index++)
        {
            var value = values[index];

            if (!float.IsFinite(value))
            {
                value = 0.0f;
            }

            copy[index] = value;
            sum += value;

            if (value >= threshold)
            {
                aboveThreshold++;
            }
        }

        Array.Sort(copy);

        return new BeatThisActivationSummary
        {
            Count = count,
            Min = copy[0],
            Max = copy[^1],
            Mean = sum / count,
            P50 = Percentile(copy, 0.50),
            P75 = Percentile(copy, 0.75),
            P90 = Percentile(copy, 0.90),
            P95 = Percentile(copy, 0.95),
            P99 = Percentile(copy, 0.99),
            AboveThresholdCount = aboveThreshold
        };
    }

    private static double Percentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0)
        {
            return 0.0;
        }

        percentile = Math.Clamp(percentile, 0.0, 1.0);

        var index = (int)Math.Round((sortedValues.Length - 1) * percentile);

        return sortedValues[Math.Clamp(index, 0, sortedValues.Length - 1)];
    }
}
