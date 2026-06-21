using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindowIntegrationTests
{
    [Fact]
    public void Shadow_candidate_set_contains_weak_windows_when_advisor_exists()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(80)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(80)));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.WeakWindows.Should().NotBeNull();
        result.CandidateSet.WeakWindows!.Status.Should().Be("evaluated");
    }

    [Fact]
    public void Shadow_candidate_set_weak_windows_not_available_when_advisor_missing()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(80)));
        var selector = new BeatTrackerSelector(builtIn);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.WeakWindows!.Status.Should().Be("not-available");
        result.CandidateSet.WeakWindows.UnreliableReason.Should().Be("advisor-not-available");
    }

    [Fact]
    public void Shadow_weak_windows_do_not_change_final_beats()
    {
        var legacyBeats = GenerateBeats(80);
        var advisorBeats = GenerateBeats(80);
        var selector = new BeatTrackerSelector(
            new RecordingBeatTracker(CreateResult("built-in", legacyBeats)),
            new RecordingBeatTracker(CreateResult("beat-this", advisorBeats)));

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.BeatTimes.Should().Equal(legacyBeats);
        result.CandidateSet!.WeakWindows!.ShouldModifyFinalGrid.Should().BeFalse();
    }

    [Fact]
    public void Shadow_weak_windows_do_not_change_selected_candidate()
    {
        var selector = new BeatTrackerSelector(
            new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(80))),
            new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(80))));

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
        result.CandidateSet.WeakWindows!.ShouldSelectAdvisor.Should().BeFalse();
    }

    [Fact]
    public void Shadow_diagnostics_exposes_weak_window_summary_fields()
    {
        var selector = new BeatTrackerSelector(
            new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(80))),
            new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(80))));

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.ShadowDiagnostics!.WeakWindowStatus.Should().Be("evaluated");
        result.ShadowDiagnostics.WeakWindowsShouldModifyFinalGrid.Should().BeFalse();
        result.ShadowDiagnostics.WeakWindowsShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Export_diagnostics_includes_weak_windows()
    {
        var selector = new BeatTrackerSelector(
            new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(80))),
            new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(80))));
        var trackingResult = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = trackingResult.CandidateSet
        };

        var export = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        export.Candidates!.WeakWindows.Should().BeSameAs(trackingResult.CandidateSet!.WeakWindows);
        export.Candidates.WeakWindows!.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
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

        public BeatTrackingResult Track(LoadedAudio audio, FeatureMatrix features, BeatTrackingOptions options)
        {
            return Result;
        }
    }
}
