using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Models;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Analysis;

public sealed class TrackAnalysisSummaryTests
{
    [Fact]
    public void From_maps_counts_and_metadata()
    {
        var analysis = CreateAnalysis();

        var summary = TrackAnalysisSummary.From(analysis);

        summary.FileHash.Should().Be("hash");
        summary.DurationSeconds.Should().Be(2.0);
        summary.SampleRate.Should().Be(22050);
        summary.Tempo.Should().Be(120.0);
        summary.TimeSignature.Should().Be(4);
        summary.SegmentCount.Should().Be(1);
        summary.BeatCount.Should().Be(1);
        summary.BarCount.Should().Be(1);
        summary.TatumCount.Should().Be(1);
        summary.SectionCount.Should().Be(1);
    }

    private static TrackAnalysis CreateAnalysis()
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "C:\\Tests\\song.wav",
                DurationSeconds = 2.0,
                SampleRate = 22050,
                Tempo = 120.0,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments =
            [
                new Segment
                {
                    Start = 0.0,
                    Duration = 0.1,
                    Confidence = 1.0,
                    LoudnessStart = 0.1,
                    LoudnessMax = 0.1,
                    LoudnessMaxTime = 0.0,
                    Timbre = new float[26],
                    Pitches = new float[12]
                }
            ],
            Beats =
            [
                new Beat
                {
                    Index = 0,
                    Start = 0.0,
                    Duration = 0.5,
                    Confidence = 1.0,
                    Timbre = new float[26],
                    Pitches = new float[12],
                    Loudness = new float[3],
                    BarPosition = [1.0f, 0.0f]
                }
            ],
            Bars =
            [
                new Bar
                {
                    Index = 0,
                    Start = 0.0,
                    Duration = 0.5,
                    Confidence = 1.0
                }
            ],
            Tatums =
            [
                new Tatum
                {
                    Index = 0,
                    Start = 0.0,
                    Duration = 0.5,
                    Confidence = 1.0
                }
            ],
            Sections =
            [
                new Section
                {
                    Index = 0,
                    Start = 0.0,
                    Duration = 2.0,
                    Confidence = 1.0,
                    Loudness = 0.0,
                    Tempo = 120.0,
                    Label = "Full Track"
                }
            ]
        };
    }
}
