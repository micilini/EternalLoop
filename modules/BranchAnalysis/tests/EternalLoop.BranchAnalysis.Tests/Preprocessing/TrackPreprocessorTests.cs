using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Preprocessing;
using EternalLoop.BranchAnalysis.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Preprocessing;

public sealed class TrackPreprocessorTests
{
    [Fact]
    public void PreprocessShouldReturnSameTrackInstance()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackAnalysisDocument result = TrackPreprocessor.Preprocess(track);

        result.Should().BeSameAs(track);
    }

    [Fact]
    public void PreprocessShouldAssignQuantumIndexesAndNavigation()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        AssertNavigation(track, track.Analysis.Sections);
        AssertNavigation(track, track.Analysis.Bars);
        AssertNavigation(track, track.Analysis.Beats);
        AssertNavigation(track, track.Analysis.Tatums);
        AssertNavigation(track, track.Analysis.Segments);
    }

    [Fact]
    public void PreprocessShouldConnectSectionsToBars()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        track.Analysis.Sections[0].Children.Should().Equal(track.Analysis.Bars);
        track.Analysis.Bars[0].Parent.Should().BeSameAs(track.Analysis.Sections[0]);
        track.Analysis.Bars[1].Parent.Should().BeSameAs(track.Analysis.Sections[0]);
    }

    [Fact]
    public void PreprocessShouldConnectBarsToBeats()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        track.Analysis.Bars[0].Children.Should().HaveCount(4);
        track.Analysis.Bars[1].Children.Should().HaveCount(4);

        for (int index = 0; index < 4; index++)
        {
            track.Analysis.Beats[index].Parent.Should().BeSameAs(track.Analysis.Bars[0]);
            track.Analysis.Beats[index].IndexInParent.Should().Be(index);
        }

        for (int index = 4; index < 8; index++)
        {
            track.Analysis.Beats[index].Parent.Should().BeSameAs(track.Analysis.Bars[1]);
            track.Analysis.Beats[index].IndexInParent.Should().Be(index - 4);
        }
    }

    [Fact]
    public void PreprocessShouldConnectBeatsToTatums()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        foreach (TimeQuantum beat in track.Analysis.Beats)
        {
            beat.Children.Should().HaveCount(2);
        }

        track.Analysis.Tatums[0].Parent.Should().BeSameAs(track.Analysis.Beats[0]);
        track.Analysis.Tatums[1].Parent.Should().BeSameAs(track.Analysis.Beats[0]);
        track.Analysis.Tatums[2].Parent.Should().BeSameAs(track.Analysis.Beats[1]);
    }

    [Fact]
    public void PreprocessShouldConnectTatumsToSegments()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        track.Analysis.Segments[0].Parent.Should().BeSameAs(track.Analysis.Tatums[0]);
        track.Analysis.Segments[1].Parent.Should().BeSameAs(track.Analysis.Tatums[0]);
        track.Analysis.Segments[2].Parent.Should().BeSameAs(track.Analysis.Tatums[1]);
        track.Analysis.Segments[3].Parent.Should().BeSameAs(track.Analysis.Tatums[2]);
    }

    [Fact]
    public void PreprocessShouldSetIndexInParent()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        track.Analysis.Segments[0].IndexInParent.Should().Be(0);
        track.Analysis.Segments[1].IndexInParent.Should().Be(1);
        track.Analysis.Segments[2].IndexInParent.Should().Be(0);
        track.Analysis.Tatums[0].IndexInParent.Should().Be(0);
        track.Analysis.Tatums[1].IndexInParent.Should().Be(1);
    }

    [Fact]
    public void PreprocessShouldCreateFirstOverlappingSegmentForBarsBeatsAndTatums()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        track.Analysis.Bars[0].Oseg.Should().BeSameAs(track.Analysis.Segments[0]);
        track.Analysis.Bars[1].Oseg.Should().BeSameAs(track.Analysis.Segments[6]);
        track.Analysis.Beats[0].Oseg.Should().BeSameAs(track.Analysis.Segments[0]);
        track.Analysis.Beats[1].Oseg.Should().BeSameAs(track.Analysis.Segments[3]);
        track.Analysis.Tatums[0].Oseg.Should().BeSameAs(track.Analysis.Segments[0]);
        track.Analysis.Tatums[2].Oseg.Should().BeSameAs(track.Analysis.Segments[3]);
    }

    [Fact]
    public void PreprocessShouldCreateAllOverlappingSegmentsForBarsBeatsAndTatums()
    {
        TrackAnalysisDocument track = AnalysisFixtureFactory.CreatePreprocessingAnalysisDocument();

        TrackPreprocessor.Preprocess(track);

        track.Analysis.Bars[0].OverlappingSegments.Should().NotBeEmpty();
        track.Analysis.Beats[0].OverlappingSegments.Should().NotBeEmpty();
        track.Analysis.Tatums[0].OverlappingSegments.Should().NotBeEmpty();

        foreach (TimeQuantum quantum in track.Analysis.Bars.Concat(track.Analysis.Beats).Concat(track.Analysis.Tatums))
        {
            foreach (SegmentQuantum segment in quantum.OverlappingSegments)
            {
                segment.End.Should().BeGreaterThanOrEqualTo(quantum.Start);
                segment.Start.Should().BeLessThanOrEqualTo(quantum.End);
            }
        }
    }

    [Fact]
    public void ConnectAllOverlappingSegmentsShouldUseInclusiveBoundaries()
    {
        TrackAnalysisDocument track = new()
        {
            Analysis = new AnalysisData
            {
                Bars = [new TimeQuantum { Start = 1, Duration = 1 }],
                Sections = [],
                Beats = [],
                Tatums = [],
                Segments =
                [
                    CreateSegment(0.5, 0.5, 0.9, [1, 1, 1]),
                    CreateSegment(2.0, 0.5, 0.9, [2, 2, 2])
                ]
            }
        };

        TrackPreprocessor.ConnectAllOverlappingSegments(track, "bars");

        track.Analysis.Bars[0].OverlappingSegments.Should().Equal(track.Analysis.Segments);
    }

    [Fact]
    public void FilterSegmentsShouldCreateFilteredSegments()
    {
        TrackAnalysisDocument track = CreateSimilarSegmentTrack();

        TrackPreprocessor.FilterSegments(track);

        track.Analysis.FilteredSegments.Should().HaveCount(2);
    }

    [Fact]
    public void FilterSegmentsShouldMergeSimilarLowConfidenceSegments()
    {
        TrackAnalysisDocument track = CreateSimilarSegmentTrack();

        TrackPreprocessor.FilterSegments(track);

        track.Analysis.FilteredSegments[0].Should().BeSameAs(track.Analysis.Segments[0]);
        track.Analysis.FilteredSegments[0].Duration.Should().Be(1.0);
        track.Analysis.FilteredSegments[1].Should().BeSameAs(track.Analysis.Segments[2]);
    }

    [Fact]
    public void FilterSegmentsShouldKeepDifferentOrHighConfidenceSegments()
    {
        TrackAnalysisDocument track = new()
        {
            Analysis = new AnalysisData
            {
                Segments =
                [
                    CreateSegment(0, 0.5, 0.9, [1, 1, 1]),
                    CreateSegment(0.5, 0.5, 0.9, [1, 1, 1]),
                    CreateSegment(1.0, 0.5, 0.1, [5, 5, 5])
                ]
            }
        };

        TrackPreprocessor.FilterSegments(track);

        track.Analysis.FilteredSegments.Should().Equal(track.Analysis.Segments);
    }

    [Fact]
    public void FilterSegmentsShouldHandleEmptySegments()
    {
        TrackAnalysisDocument track = new()
        {
            Analysis = new AnalysisData
            {
                Segments = []
            }
        };

        TrackPreprocessor.FilterSegments(track);

        track.Analysis.FilteredSegments.Should().BeEmpty();
    }

    [Fact]
    public void TimbralDistanceShouldUseFirstThreeTimbreValues()
    {
        double distance = TrackPreprocessor.TimbralDistance(
            CreateSegment(0, 1, 1, [1, 2, 3, 999]),
            CreateSegment(0, 1, 1, [1, 2, 4, -999]));

        distance.Should().Be(1);
    }

    [Fact]
    public void EuclideanDistanceShouldReturnPositiveInfinityForShortVectors()
    {
        double distance = TrackPreprocessor.EuclideanDistance([1, 2], [1, 2], 3);

        distance.Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void EuclideanDistanceShouldTreatNonFiniteValuesAsZero()
    {
        double distance = TrackPreprocessor.EuclideanDistance([double.NaN, 2, 3], [1, double.PositiveInfinity, 3], 3);

        distance.Should().Be(Math.Sqrt(5));
    }

    [Fact]
    public void PreprocessShouldRejectNullTrack()
    {
        Action act = () => TrackPreprocessor.Preprocess(null!);

        act.Should().Throw<TrackPreprocessorException>()
            .WithMessage("Track must be an object.");
    }

    [Fact]
    public void PreprocessShouldRejectMissingAnalysis()
    {
        TrackAnalysisDocument track = new()
        {
            Analysis = null!
        };

        Action act = () => TrackPreprocessor.Preprocess(track);

        act.Should().Throw<TrackPreprocessorException>()
            .WithMessage("Track analysis must be an object.");
    }

    private static void AssertNavigation<TQuantum>(TrackAnalysisDocument track, IList<TQuantum> quanta)
        where TQuantum : TimeQuantum
    {
        for (int index = 0; index < quanta.Count; index++)
        {
            quanta[index].Track.Should().BeSameAs(track);
            quanta[index].Which.Should().Be(index);
        }

        quanta[0].Prev.Should().BeNull();
        quanta[0].Next.Should().BeSameAs(quanta.Count > 1 ? quanta[1] : null);
        quanta[^1].Next.Should().BeNull();
    }

    private static TrackAnalysisDocument CreateSimilarSegmentTrack()
    {
        return new TrackAnalysisDocument
        {
            Analysis = new AnalysisData
            {
                Segments =
                [
                    CreateSegment(0, 0.5, 0.9, [1, 1, 1]),
                    CreateSegment(0.5, 0.5, 0.1, [1, 1, 1]),
                    CreateSegment(1.0, 0.5, 0.9, [5, 5, 5])
                ]
            }
        };
    }

    private static SegmentQuantum CreateSegment(double start, double duration, double confidence, List<double> timbre)
    {
        return new SegmentQuantum
        {
            Start = start,
            Duration = duration,
            Confidence = confidence,
            LoudnessStart = 0,
            LoudnessMax = 0,
            LoudnessMaxTime = 0,
            Timbre = timbre,
            Pitches = []
        };
    }
}
