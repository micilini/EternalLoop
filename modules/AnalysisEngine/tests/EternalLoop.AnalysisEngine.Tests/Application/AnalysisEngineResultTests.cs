using EternalLoop.AnalysisEngine.Core.Application;
using EternalLoop.AnalysisEngine.Core.Models;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Application;

public sealed class AnalysisEngineResultTests
{
    [Fact]
    public void ConstructorShouldCreateSummaryFromAnalysis()
    {
        var analysis = CreateAnalysis();

        var result = new AnalysisEngineResult(analysis);

        result.Analysis.Should().BeSameAs(analysis);
        result.Summary.Duration.Should().Be(TimeSpan.FromSeconds(120));
        result.Summary.Tempo.Should().Be(128);
        result.Summary.SampleRate.Should().Be(22050);
        result.Summary.BeatCount.Should().Be(2);
        result.Summary.BarCount.Should().Be(1);
        result.Summary.TatumCount.Should().Be(4);
        result.Summary.SegmentCount.Should().Be(1);
        result.Summary.SectionCount.Should().Be(1);
        result.Summary.HasBeats.Should().BeTrue();
        result.Summary.HasSegments.Should().BeTrue();
    }

    [Fact]
    public void ConstructorShouldRejectNullAnalysis()
    {
        var act = () => new AnalysisEngineResult(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static TrackAnalysis CreateAnalysis()
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "track.mp3",
                DurationSeconds = 120,
                SampleRate = 22050,
                Tempo = 128,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Beats =
            [
                new Beat
                {
                    Index = 0,
                    Start = 0,
                    Duration = 0.5,
                    Confidence = 1,
                    Timbre = [0],
                    Pitches = [0],
                    Loudness = [0],
                    BarPosition = [1, 0, 0, 0]
                },
                new Beat
                {
                    Index = 1,
                    Start = 0.5,
                    Duration = 0.5,
                    Confidence = 1,
                    Timbre = [0],
                    Pitches = [0],
                    Loudness = [0],
                    BarPosition = [0, 1, 0, 0]
                }
            ],
            Bars =
            [
                new Bar
                {
                    Index = 0,
                    Start = 0,
                    Duration = 2,
                    Confidence = 1
                }
            ],
            Tatums =
            [
                new Tatum { Index = 0, Start = 0, Duration = 0.25, Confidence = 1 },
                new Tatum { Index = 1, Start = 0.25, Duration = 0.25, Confidence = 1 },
                new Tatum { Index = 2, Start = 0.5, Duration = 0.25, Confidence = 1 },
                new Tatum { Index = 3, Start = 0.75, Duration = 0.25, Confidence = 1 }
            ],
            Segments =
            [
                new Segment
                {
                    Start = 0,
                    Duration = 1,
                    Confidence = 1,
                    LoudnessStart = -10,
                    LoudnessMax = -5,
                    LoudnessMaxTime = 0.1,
                    Timbre = [0],
                    Pitches = [0]
                }
            ],
            Sections =
            [
                new Section
                {
                    Index = 0,
                    Start = 0,
                    Duration = 120,
                    Confidence = 1,
                    Loudness = -5,
                    Tempo = 128,
                    Label = "A"
                }
            ]
        };
    }
}
