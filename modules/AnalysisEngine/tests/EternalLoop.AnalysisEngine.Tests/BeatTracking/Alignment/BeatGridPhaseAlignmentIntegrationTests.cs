using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentIntegrationTests
{
    [Fact]
    public void Shadow_candidate_set_contains_phase_alignment_when_advisor_exists()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(64).Select(beat => beat + 0.040).ToArray()));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.PhaseAlignment.Should().NotBeNull();
        result.CandidateSet.PhaseAlignment!.BestOffsetMs.Should().Be(-40.0);
        result.CandidateSet.PhaseAlignment.Status.Should().Be("offset-detected");
    }

    [Fact]
    public void Shadow_candidate_set_phase_alignment_not_available_when_advisor_missing()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var selector = new BeatTrackerSelector(builtIn);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.PhaseAlignment.Should().NotBeNull();
        result.CandidateSet.PhaseAlignment!.Status.Should().Be("not-available");
        result.CandidateSet.PhaseAlignment.UnreliableReason.Should().Be("advisor-not-available");
    }

    [Fact]
    public void Shadow_phase_alignment_does_not_change_final_beats()
    {
        var legacyBeats = GenerateBeats(64);
        var advisorBeats = legacyBeats.Select(beat => beat + 0.040).ToArray();
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", legacyBeats));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", advisorBeats));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.BeatTimes.Should().Equal(legacyBeats);
        result.CandidateSet!.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
    }

    [Fact]
    public void Shadow_phase_alignment_should_apply_correction_false()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(64).Select(beat => beat + 0.040).ToArray()));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.PhaseAlignment!.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Shadow_diagnostics_exposes_phase_alignment_summary_fields()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(64).Select(beat => beat + 0.040).ToArray()));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.ShadowDiagnostics!.PhaseAlignmentStatus.Should().Be("offset-detected");
        result.ShadowDiagnostics.PhaseAlignmentBestOffsetMs.Should().Be(-40.0);
        result.ShadowDiagnostics.PhaseAlignmentConfidence.Should().Be("High");
        result.ShadowDiagnostics.PhaseAlignmentShouldApplyCorrection.Should().BeFalse();
    }

    private static BeatTrackingOptions CreateShadowOptions()
    {
        return new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.Shadow };
    }

    private static BeatTrackingResult CreateResult(string provider, double[] beats)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = beats,
            Confidences = Enumerable.Repeat(0.9, beats.Length).ToArray(),
            ProviderName = provider,
            UsedAiProvider = provider == "beat-this",
            UsedBuiltInProvider = provider == "built-in",
            BeatGridMode = provider
        };
    }

    private static double[] GenerateBeats(int count)
    {
        return Enumerable.Range(0, count).Select(index => index * 0.5).ToArray();
    }

    private static LoadedAudio CreateAudio()
    {
        return new LoadedAudio([], 22050, 40.0, "hash", "song.wav", "song.wav");
    }

    private static FeatureMatrix CreateFeatures()
    {
        return new FeatureMatrix
        {
            Mfcc = [],
            Chroma = [],
            SpectralFlux = [],
            Rms = [],
            FrameSizeSamples = 2048,
            HopLengthSamples = 512,
            SampleRate = 22050
        };
    }

    private sealed class RecordingBeatTracker : IBeatTracker
    {
        public RecordingBeatTracker(BeatTrackingResult result)
        {
            Result = result;
        }

        public BeatTrackingResult Result { get; }

        public BeatTrackingResult Track(
            LoadedAudio audio,
            FeatureMatrix features,
            BeatTrackingOptions options)
        {
            return Result;
        }
    }
}
