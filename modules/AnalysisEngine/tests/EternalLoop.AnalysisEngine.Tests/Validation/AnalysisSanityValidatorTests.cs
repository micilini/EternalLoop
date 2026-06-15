using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Validation;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Validation;

public sealed class AnalysisSanityValidatorTests
{
    [Fact]
    public void Validate_accepts_valid_analysis()
    {
        var validator = new AnalysisSanityValidator();
        var analysis = CreateValidAnalysis();

        var result = validator.Validate(analysis);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_rejects_empty_segments()
    {
        var validator = new AnalysisSanityValidator();
        var analysis = CreateValidAnalysis(segments: []);

        var result = validator.Validate(analysis);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("segment", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_empty_beats()
    {
        var validator = new AnalysisSanityValidator();
        var analysis = CreateValidAnalysis(beats: []);

        var result = validator.Validate(analysis);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("beat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_invalid_metadata_duration()
    {
        var validator = new AnalysisSanityValidator();
        var analysis = CreateValidAnalysis(durationSeconds: 0.0);

        var result = validator.Validate(analysis);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("duration", StringComparison.OrdinalIgnoreCase));
    }

    private static TrackAnalysis CreateValidAnalysis(
        IReadOnlyList<Segment>? segments = null,
        IReadOnlyList<Beat>? beats = null,
        double durationSeconds = 2.0)
    {
        var effectiveBeats = beats ?? CreateBeats();

        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "C:\\Tests\\song.wav",
                DurationSeconds = durationSeconds,
                SampleRate = 22050,
                Tempo = 120.0,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = segments ?? CreateSegments(),
            Beats = effectiveBeats,
            Bars =
            [
                new Bar
                {
                    Index = 0,
                    Start = 0.0,
                    Duration = 1.0,
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

    private static IReadOnlyList<Segment> CreateSegments()
    {
        return
        [
            new Segment
            {
                Start = 0.0,
                Duration = 0.1,
                Confidence = 1.0,
                LoudnessStart = 0.1,
                LoudnessMax = 0.2,
                LoudnessMaxTime = 0.0,
                Timbre = new float[26],
                Pitches = new float[12]
            }
        ];
    }

    private static IReadOnlyList<Beat> CreateBeats()
    {
        return
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
        ];
    }
}
