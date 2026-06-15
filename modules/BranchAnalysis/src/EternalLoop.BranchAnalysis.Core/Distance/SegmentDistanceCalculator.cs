using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Distance;

public static class SegmentDistanceCalculator
{
    public const string TimbreField = "timbre";
    public const string PitchesField = "pitches";

    public static double EuclideanDistance(IReadOnlyList<double>? vectorA, IReadOnlyList<double>? vectorB)
    {
        ValidateVectorPair(vectorA, vectorB);

        double sum = 0;

        for (int index = 0; index < vectorA!.Count; index++)
        {
            double delta = ToFiniteNumber(vectorB![index]) - ToFiniteNumber(vectorA[index]);
            sum += delta * delta;
        }

        return Math.Sqrt(sum);
    }

    public static double WeightedEuclideanDistance(IReadOnlyList<double>? vectorA, IReadOnlyList<double>? vectorB)
    {
        return EuclideanDistance(vectorA, vectorB);
    }

    public static double SegmentDistance(
        SegmentQuantum? segmentA,
        SegmentQuantum? segmentB,
        string field,
        bool weighted = false)
    {
        IReadOnlyList<double> valuesA = GetValidatedSegmentField(segmentA, field);
        IReadOnlyList<double> valuesB = GetValidatedSegmentField(segmentB, field);

        return weighted
            ? WeightedEuclideanDistance(valuesA, valuesB)
            : EuclideanDistance(valuesA, valuesB);
    }

    public static double GetSegmentDistances(
        SegmentQuantum? segmentA,
        SegmentQuantum? segmentB,
        SegmentDistanceWeights? weights = null)
    {
        SegmentDistanceWeights effectiveWeights = weights ?? SegmentDistanceWeights.Default;
        ValidateSegment(segmentA);
        ValidateSegment(segmentB);

        double timbre = SegmentDistance(segmentA, segmentB, TimbreField, weighted: true);
        double pitch = SegmentDistance(segmentA, segmentB, PitchesField);
        double loudStart = Math.Abs(ToFiniteNumber(segmentA!.LoudnessStart) - ToFiniteNumber(segmentB!.LoudnessStart));
        double loudMax = Math.Abs(ToFiniteNumber(segmentA.LoudnessMax) - ToFiniteNumber(segmentB.LoudnessMax));
        double duration = Math.Abs(ToFiniteNumber(segmentA.Duration) - ToFiniteNumber(segmentB.Duration));
        double confidence = Math.Abs(ToFiniteNumber(segmentA.Confidence) - ToFiniteNumber(segmentB.Confidence));

        return timbre * effectiveWeights.Timbre
            + pitch * effectiveWeights.Pitch
            + loudStart * effectiveWeights.LoudStart
            + loudMax * effectiveWeights.LoudMax
            + duration * effectiveWeights.Duration
            + confidence * effectiveWeights.Confidence;
    }

    private static void ValidateSegment(SegmentQuantum? segment)
    {
        if (segment is null)
        {
            throw new SegmentDistanceException("Segment must be an object.");
        }

        ValidateSegmentField(segment, TimbreField);
        ValidateSegmentField(segment, PitchesField);
        ToFiniteNumber(segment.LoudnessStart);
        ToFiniteNumber(segment.LoudnessMax);
        ToFiniteNumber(segment.Duration);
        ToFiniteNumber(segment.Confidence);
    }

    private static IReadOnlyList<double> GetValidatedSegmentField(SegmentQuantum? segment, string field)
    {
        ValidateSegmentField(segment, field);

        return field switch
        {
            TimbreField => segment!.Timbre,
            PitchesField => segment!.Pitches,
            _ => throw new SegmentDistanceException($"Unsupported segment field: {field}")
        };
    }

    private static void ValidateSegmentField(SegmentQuantum? segment, string field)
    {
        if (segment is null)
        {
            throw new SegmentDistanceException("Segment must be an object.");
        }

        IReadOnlyList<double>? values = field switch
        {
            TimbreField => segment.Timbre,
            PitchesField => segment.Pitches,
            _ => throw new SegmentDistanceException($"Unsupported segment field: {field}")
        };

        if (values is null)
        {
            throw new SegmentDistanceException($"Segment {field} must be an array.");
        }

        if (values.Count == 0)
        {
            throw new SegmentDistanceException($"Segment {field} cannot be empty.");
        }
    }

    private static void ValidateVectorPair(IReadOnlyList<double>? vectorA, IReadOnlyList<double>? vectorB)
    {
        if (vectorA is null || vectorB is null)
        {
            throw new SegmentDistanceException("Both vectors must be arrays.");
        }

        if (vectorA.Count == 0 || vectorB.Count == 0)
        {
            throw new SegmentDistanceException("Vectors cannot be empty.");
        }

        if (vectorA.Count != vectorB.Count)
        {
            throw new SegmentDistanceException("Vectors must have the same length.");
        }

        foreach (double value in vectorA)
        {
            ToFiniteNumber(value);
        }

        foreach (double value in vectorB)
        {
            ToFiniteNumber(value);
        }
    }

    private static double ToFiniteNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new SegmentDistanceException("Expected a finite number.");
        }

        return value;
    }
}
