using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionIntegrationTests
{
    [Fact]
    public void Shadow_candidate_set_contains_correction_diagnostics()
    {
        var selector = CreateSelector(CreateRegularBeats(), CreateRegularBeats());

        var result = selector.Track(CreateAudio(), CreateFeatures(), ShadowOptions());

        result.CandidateSet!.WeakWindowCorrections.Should().NotBeNull();
    }

    [Fact]
    public void Shadow_with_ready_weak_window_contains_corrected_experimental_candidate()
    {
        var selector = CreateSelector(CreateIrregularLegacyBeats(), CreateRegularBeats());

        var result = selector.Track(CreateAudio(), CreateFeatures(), ShadowOptions());

        result.CandidateSet!.CorrectedExperimental.Should().NotBeNull();
        result.CandidateSet.All.Should().Contain(candidate => candidate.Id == "weak-window-corrected-experimental");
        result.CandidateSet.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
    }

    [Fact]
    public void Shadow_without_ready_weak_window_has_no_corrected_candidate()
    {
        var selector = CreateSelector(CreateRegularBeats(), CreateRegularBeats());

        var result = selector.Track(CreateAudio(), CreateFeatures(), ShadowOptions());

        result.CandidateSet!.CorrectedExperimental.Should().BeNull();
    }

    [Fact]
    public void Shadow_final_beats_remain_legacy()
    {
        var legacy = CreateIrregularLegacyBeats();
        var selector = CreateSelector(legacy, CreateRegularBeats());

        var result = selector.Track(CreateAudio(), CreateFeatures(), ShadowOptions());

        result.BeatTimes.Should().Equal(legacy);
    }

    [Fact]
    public void Export_diagnostics_includes_weak_window_corrections()
    {
        var trackingResult = CreateSelector(CreateIrregularLegacyBeats(), CreateRegularBeats())
            .Track(CreateAudio(), CreateFeatures(), ShadowOptions());
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = trackingResult.CandidateSet
        };

        var export = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        export.Candidates!.WeakWindowCorrections.Should().BeSameAs(trackingResult.CandidateSet!.WeakWindowCorrections);
        export.Candidates.CorrectedExperimental.Should().BeSameAs(trackingResult.CandidateSet.CorrectedExperimental);
    }

    public static BeatTrackerSelector CreateSelector(double[] legacyBeats, double[] advisorBeats)
    {
        return new BeatTrackerSelector(
            new RecordingBeatTracker(CreateResult("built-in", legacyBeats)),
            new RecordingBeatTracker(CreateResult("beat-this", advisorBeats)));
    }

    public static double[] CreateRegularBeats()
    {
        return Enumerable.Range(0, 80).Select(index => index * 0.5).ToArray();
    }

    public static double[] CreateIrregularLegacyBeats()
    {
        var beats = CreateRegularBeats();
        beats[20] += 0.35;
        beats[21] += 0.20;
        beats[22] -= 0.20;
        beats[23] += 0.30;
        return beats;
    }

    public static BeatTrackingOptions ShadowOptions()
    {
        return new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.Shadow };
    }

    public static LoadedAudio CreateAudio()
    {
        return new LoadedAudio([], 22050, 45.0, "hash", "song.wav", "song.wav");
    }

    public static FeatureMatrix CreateFeatures()
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

    private sealed class RecordingBeatTracker : IBeatTracker
    {
        public RecordingBeatTracker(BeatTrackingResult result) => Result = result;

        public BeatTrackingResult Result { get; }

        public BeatTrackingResult Track(LoadedAudio audio, FeatureMatrix features, BeatTrackingOptions options) => Result;
    }
}
