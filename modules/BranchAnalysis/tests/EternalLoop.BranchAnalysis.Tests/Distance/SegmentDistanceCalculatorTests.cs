using EternalLoop.BranchAnalysis.Core.Distance;
using EternalLoop.BranchAnalysis.Core.Models;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Distance;

public sealed class SegmentDistanceCalculatorTests
{
    [Fact]
    public void EuclideanDistanceShouldCalculateSimpleDistance()
    {
        double distance = SegmentDistanceCalculator.EuclideanDistance([0, 0], [3, 4]);

        distance.Should().Be(5);
    }

    [Fact]
    public void WeightedEuclideanDistanceShouldMatchNodeBehavior()
    {
        double distance = SegmentDistanceCalculator.WeightedEuclideanDistance([1, 2, 3], [2, 4, 6]);

        distance.Should().Be(Math.Sqrt(14));
    }

    [Fact]
    public void SegmentDistanceShouldCalculateTimbreDistance()
    {
        SegmentQuantum segmentA = CreateSegment(timbre: [0, 0]);
        SegmentQuantum segmentB = CreateSegment(timbre: [3, 4]);

        double distance = SegmentDistanceCalculator.SegmentDistance(segmentA, segmentB, SegmentDistanceCalculator.TimbreField);

        distance.Should().Be(5);
    }

    [Fact]
    public void SegmentDistanceShouldCalculatePitchesDistance()
    {
        SegmentQuantum segmentA = CreateSegment(pitches: [0, 0]);
        SegmentQuantum segmentB = CreateSegment(pitches: [1, 1]);

        double distance = SegmentDistanceCalculator.SegmentDistance(segmentA, segmentB, SegmentDistanceCalculator.PitchesField);

        distance.Should().Be(Math.Sqrt(2));
    }

    [Fact]
    public void GetSegmentDistancesShouldApplyDefaultWeights()
    {
        SegmentQuantum segmentA = CreateSegment(
            timbre: [0, 0],
            pitches: [0, 0],
            loudnessStart: -10,
            loudnessMax: -5,
            duration: 1,
            confidence: 0.5);
        SegmentQuantum segmentB = CreateSegment(
            timbre: [3, 4],
            pitches: [1, 1],
            loudnessStart: -8,
            loudnessMax: -4,
            duration: 1.5,
            confidence: 0.25);

        double distance = SegmentDistanceCalculator.GetSegmentDistances(segmentA, segmentB);

        distance.Should().BeApproximately(58.25 + Math.Sqrt(2) * 10, 0.0000001);
    }

    [Fact]
    public void GetSegmentDistancesShouldAcceptCustomWeights()
    {
        SegmentQuantum segmentA = CreateSegment(timbre: [0, 0], pitches: [0, 0]);
        SegmentQuantum segmentB = CreateSegment(timbre: [3, 4], pitches: [100, 100]);
        SegmentDistanceWeights weights = new()
        {
            Timbre = 2,
            Pitch = 0,
            LoudStart = 0,
            LoudMax = 0,
            Duration = 0,
            Confidence = 0
        };

        double distance = SegmentDistanceCalculator.GetSegmentDistances(segmentA, segmentB, weights);

        distance.Should().Be(10);
    }

    [Fact]
    public void GetSegmentDistancesShouldReturnZeroForIdenticalSegments()
    {
        SegmentQuantum segment = CreateSegment();

        double distance = SegmentDistanceCalculator.GetSegmentDistances(segment, segment);

        distance.Should().Be(0);
    }

    [Fact]
    public void GetSegmentDistancesShouldApplyPitchWeight()
    {
        SegmentQuantum segmentA = CreateSegment(pitches: [0, 0, 0]);
        SegmentQuantum segmentB = CreateSegment(pitches: [1, 0, 0]);

        double distance = SegmentDistanceCalculator.GetSegmentDistances(segmentA, segmentB);

        distance.Should().Be(SegmentDistanceWeights.Default.Pitch);
    }

    [Fact]
    public void GetSegmentDistancesShouldApplyDurationWeight()
    {
        SegmentQuantum segmentA = CreateSegment(duration: 1);
        SegmentQuantum segmentB = CreateSegment(duration: 1.5);

        double distance = SegmentDistanceCalculator.GetSegmentDistances(segmentA, segmentB);

        distance.Should().Be(50);
    }

    [Fact]
    public void GetSegmentDistancesShouldHandleFullTimbreVector()
    {
        List<double> timbreA = Enumerable.Repeat(0.0, 26).ToList();
        List<double> timbreB = Enumerable.Repeat(0.0, 26).ToList();
        timbreB[25] = 2;

        double distance = SegmentDistanceCalculator.GetSegmentDistances(
            CreateSegment(timbre: timbreA),
            CreateSegment(timbre: timbreB));

        distance.Should().Be(2);
    }

    [Fact]
    public void GetSegmentDistancesShouldHandleFullPitchVector()
    {
        List<double> pitchesA = Enumerable.Repeat(0.0, 12).ToList();
        List<double> pitchesB = Enumerable.Repeat(0.0, 12).ToList();
        pitchesB[11] = 0.5;

        double distance = SegmentDistanceCalculator.GetSegmentDistances(
            CreateSegment(pitches: pitchesA),
            CreateSegment(pitches: pitchesB));

        distance.Should().Be(5);
    }

    [Fact]
    public void GetSegmentDistancesShouldIncludeLoudnessAndConfidenceTerms()
    {
        SegmentQuantum segmentA = CreateSegment(loudnessStart: 1, loudnessMax: 2, confidence: 0.25);
        SegmentQuantum segmentB = CreateSegment(loudnessStart: 3, loudnessMax: 5, confidence: 0.75);

        double distance = SegmentDistanceCalculator.GetSegmentDistances(segmentA, segmentB);

        distance.Should().Be(5.5);
    }

    [Fact]
    public void EuclideanDistanceShouldRejectNullVectors()
    {
        Action act = () => SegmentDistanceCalculator.EuclideanDistance(null, [1]);

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Both vectors must be arrays.");
    }

    [Fact]
    public void EuclideanDistanceShouldRejectEmptyVectors()
    {
        Action act = () => SegmentDistanceCalculator.EuclideanDistance([], []);

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Vectors cannot be empty.");
    }

    [Fact]
    public void EuclideanDistanceShouldRejectDifferentVectorLengths()
    {
        Action act = () => SegmentDistanceCalculator.EuclideanDistance([1], [1, 2]);

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Vectors must have the same length.");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void EuclideanDistanceShouldRejectNonFiniteValues(double value)
    {
        Action act = () => SegmentDistanceCalculator.EuclideanDistance([value], [1]);

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Expected a finite number.");
    }

    [Fact]
    public void GetSegmentDistancesShouldRejectNullSegment()
    {
        SegmentQuantum segment = CreateSegment();

        Action act = () => SegmentDistanceCalculator.GetSegmentDistances(null, segment);

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Segment must be an object.");
    }

    [Fact]
    public void GetSegmentDistancesShouldRejectMissingTimbre()
    {
        SegmentQuantum segment = CreateSegment();
        segment.Timbre = null!;

        Action act = () => SegmentDistanceCalculator.GetSegmentDistances(segment, CreateSegment());

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Segment timbre must be an array.");
    }

    [Fact]
    public void GetSegmentDistancesShouldRejectEmptyTimbre()
    {
        SegmentQuantum segment = CreateSegment(timbre: []);

        Action act = () => SegmentDistanceCalculator.GetSegmentDistances(segment, CreateSegment());

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Segment timbre cannot be empty.");
    }

    [Fact]
    public void GetSegmentDistancesShouldRejectMissingPitches()
    {
        SegmentQuantum segment = CreateSegment();
        segment.Pitches = null!;

        Action act = () => SegmentDistanceCalculator.GetSegmentDistances(segment, CreateSegment());

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Segment pitches must be an array.");
    }

    [Fact]
    public void GetSegmentDistancesShouldRejectEmptyPitches()
    {
        SegmentQuantum segment = CreateSegment(pitches: []);

        Action act = () => SegmentDistanceCalculator.GetSegmentDistances(segment, CreateSegment());

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Segment pitches cannot be empty.");
    }

    [Fact]
    public void SegmentDistanceShouldRejectUnknownField()
    {
        Action act = () => SegmentDistanceCalculator.SegmentDistance(CreateSegment(), CreateSegment(), "energy");

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Unsupported segment field: energy");
    }

    [Theory]
    [InlineData(nameof(SegmentQuantum.LoudnessStart), double.NaN)]
    [InlineData(nameof(SegmentQuantum.LoudnessMax), double.PositiveInfinity)]
    [InlineData(nameof(SegmentQuantum.Duration), double.NaN)]
    [InlineData(nameof(SegmentQuantum.Confidence), double.NegativeInfinity)]
    public void GetSegmentDistancesShouldRejectNonFiniteScalarFields(string field, double value)
    {
        SegmentQuantum segment = CreateSegment();
        SetScalarField(segment, field, value);

        Action act = () => SegmentDistanceCalculator.GetSegmentDistances(segment, CreateSegment());

        act.Should().Throw<SegmentDistanceException>()
            .WithMessage("Expected a finite number.");
    }

    private static SegmentQuantum CreateSegment(
        List<double>? timbre = null,
        List<double>? pitches = null,
        double loudnessStart = 0,
        double loudnessMax = 0,
        double duration = 1,
        double confidence = 1)
    {
        return new SegmentQuantum
        {
            Start = 0,
            Duration = duration,
            Confidence = confidence,
            LoudnessStart = loudnessStart,
            LoudnessMax = loudnessMax,
            LoudnessMaxTime = 0,
            Timbre = timbre ?? [0, 0, 0],
            Pitches = pitches ?? [0, 0, 0]
        };
    }

    private static void SetScalarField(SegmentQuantum segment, string field, double value)
    {
        switch (field)
        {
            case nameof(SegmentQuantum.LoudnessStart):
                segment.LoudnessStart = value;
                break;
            case nameof(SegmentQuantum.LoudnessMax):
                segment.LoudnessMax = value;
                break;
            case nameof(SegmentQuantum.Duration):
                segment.Duration = value;
                break;
            case nameof(SegmentQuantum.Confidence):
                segment.Confidence = value;
                break;
        }
    }
}
