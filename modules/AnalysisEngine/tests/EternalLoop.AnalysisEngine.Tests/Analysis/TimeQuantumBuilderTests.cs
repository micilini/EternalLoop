using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Analysis;

public sealed class TimeQuantumBuilderTests
{
    [Fact]
    public void BuildBars_groups_beats_by_time_signature()
    {
        var beats = CreateBeats(8);

        var bars = TimeQuantumBuilder.BuildBars(beats, timeSignature: 4);

        bars.Should().HaveCount(2);
        bars[0].Start.Should().Be(0.0);
        bars[1].Start.Should().Be(2.0);
    }

    [Fact]
    public void BuildBars_keeps_positive_durations()
    {
        var beats = CreateBeats(8);

        var bars = TimeQuantumBuilder.BuildBars(beats, timeSignature: 4);

        bars.Should().OnlyContain(bar => bar.Duration > 0.0);
    }

    [Fact]
    public void BuildTatums_creates_two_tatums_per_beat()
    {
        var beats = CreateBeats(6);

        var tatums = TimeQuantumBuilder.BuildTatums(beats);

        tatums.Should().HaveCount(beats.Count * 2);
        tatums.Where((_, index) => index % 2 == 0).Select(tatum => tatum.Start).Should().Equal(beats.Select(beat => beat.Start));
    }

    [Fact]
    public void BuildTatums_keeps_positive_ordered_durations()
    {
        var beats = CreateBeats(6);

        var tatums = TimeQuantumBuilder.BuildTatums(beats);

        tatums.Should().OnlyContain(tatum => tatum.Duration > 0.0);
        tatums.Select(tatum => tatum.Start).Should().BeInAscendingOrder();
    }

    [Fact]
    public void BuildTatums_with_adaptive_evidence_uses_internal_peaks()
    {
        var beats = CreateBeats(2);
        var odf = new float[60];
        odf[10] = 1.0f;
        odf[20] = 1.0f;

        var tatums = TimeQuantumBuilder.BuildTatums(
            beats,
            odf,
            framesPerSecond: 60.0,
            adaptiveTatums: true,
            evidenceConfidences: true);

        tatums.Should().HaveCountGreaterThan(beats.Count * 2);
        tatums.Select(tatum => tatum.Start).Should().BeInAscendingOrder();
        tatums.Should().OnlyContain(tatum => tatum.Duration > 0.0);
    }

    [Fact]
    public void BuildSections_uses_bars_for_long_tracks()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 128.0);
        var bars = TimeQuantumBuilder.BuildBars(CreateBeats(256), timeSignature: 4);

        var sections = TimeQuantumBuilder.BuildSections(audio, bars, tempo: 120.0);

        sections.Should().HaveCountGreaterThan(1);
        sections.Should().HaveCountLessThanOrEqualTo(16);
        var lastSection = sections[^1];
        sections.Should().OnlyContain(section => section.Duration >= 8.0 || ReferenceEquals(section, lastSection));
    }

    [Fact]
    public void BuildSections_with_structural_evidence_sets_loudness_and_confidence()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 128.0);
        var beats = CreateBeats(256);
        var bars = TimeQuantumBuilder.BuildBars(beats, timeSignature: 4);

        var sections = TimeQuantumBuilder.BuildSections(
            audio,
            bars,
            beats,
            tempo: 120.0,
            timeSignature: 4,
            structuralSections: true,
            evidenceConfidences: true);

        sections.Should().OnlyContain(section => section.Duration > 0.0 && section.Tempo > 0.0);
        sections.Select(section => section.Confidence).Should().OnlyContain(confidence => confidence >= 0.0 && confidence <= 1.0);
    }

    [Fact]
    public void BuildSections_with_structural_evidence_respects_minimum_count_guardrail()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 220.0);
        var beats = CreateBeats(450);
        var bars = TimeQuantumBuilder.BuildBars(beats, timeSignature: 4);

        var sections = TimeQuantumBuilder.BuildSections(
            audio,
            bars,
            beats,
            tempo: 123.0,
            timeSignature: 4,
            structuralSections: true,
            evidenceConfidences: true);

        sections.Should().HaveCountGreaterThanOrEqualTo((int)Math.Ceiling(audio.DurationSeconds / 45.0));
    }

    [Fact]
    public void BuildSections_preserves_single_full_track_fallback_for_short_tracks()
    {
        var audio = TestSignalFactory.CreateSilentLoadedAudio(durationSeconds: 5.0);
        var bars = TimeQuantumBuilder.BuildBars(CreateBeats(8), timeSignature: 4);

        var sections = TimeQuantumBuilder.BuildSections(audio, bars, tempo: 120.0);

        sections.Should().ContainSingle();
        sections[0].Start.Should().Be(0.0);
        sections[0].Duration.Should().Be(audio.DurationSeconds);
        sections[0].Tempo.Should().Be(120.0);
        sections[0].Label.Should().Be("Full Track");
    }

    private static IReadOnlyList<Beat> CreateBeats(int count)
    {
        var beats = new List<Beat>();

        for (var index = 0; index < count; index++)
        {
            beats.Add(new Beat
            {
                Index = index,
                Start = index * 0.5,
                Duration = 0.5,
                Confidence = 0.8,
                Timbre = new float[26],
                Pitches = new float[12],
                Loudness = new float[3],
                BarPosition = [0.0f, 1.0f]
            });
        }

        return beats;
    }
}
