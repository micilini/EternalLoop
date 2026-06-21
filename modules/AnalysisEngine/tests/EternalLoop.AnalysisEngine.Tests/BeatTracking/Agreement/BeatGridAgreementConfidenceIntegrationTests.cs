using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceIntegrationTests
{
    [Fact]
    public void Shadow_candidate_set_contains_agreement_confidence_when_advisor_exists()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(64)));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.AgreementConfidence.Should().NotBeNull();
        result.CandidateSet.AgreementConfidence!.GlobalConfidence!.Level.Should().BeOneOf(
            BeatGridAgreementConfidenceLevel.High,
            BeatGridAgreementConfidenceLevel.VeryHigh);
    }

    [Fact]
    public void Shadow_candidate_set_agreement_confidence_not_available_when_advisor_missing()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var selector = new BeatTrackerSelector(builtIn);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.AgreementConfidence.Should().NotBeNull();
        result.CandidateSet.AgreementConfidence!.Status.Should().Be("not-available");
        result.CandidateSet.AgreementConfidence.UnreliableReason.Should().Be("advisor-not-available");
    }

    [Fact]
    public void Shadow_agreement_confidence_does_not_change_final_beats()
    {
        var legacyBeats = GenerateBeats(64);
        var advisorBeats = legacyBeats.Select(beat => beat + 0.040).ToArray();
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", legacyBeats));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", advisorBeats));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.BeatTimes.Should().Equal(legacyBeats);
        result.CandidateSet!.AgreementConfidence!.ShouldModifyFinalGrid.Should().BeFalse();
    }

    [Fact]
    public void Shadow_agreement_confidence_does_not_change_selected_candidate()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(64)));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.CandidateSet!.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
        result.CandidateSet.AgreementConfidence!.ShouldSelectAdvisor.Should().BeFalse();
    }

    [Fact]
    public void Shadow_diagnostics_exposes_agreement_confidence_summary_fields()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(64)));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());

        result.ShadowDiagnostics!.AgreementConfidenceStatus.Should().Be("evaluated");
        result.ShadowDiagnostics.AgreementConfidenceLevel.Should().BeOneOf("High", "VeryHigh");
        result.ShadowDiagnostics.FutureFusionReadiness.Should().Be("candidate-ready");
        result.ShadowDiagnostics.AgreementShouldModifyFinalGrid.Should().BeFalse();
        result.ShadowDiagnostics.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }

    [Fact]
    public void Export_diagnostics_includes_agreement_confidence()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", GenerateBeats(64)));
        var beatThis = new RecordingBeatTracker(CreateResult("beat-this", GenerateBeats(64)));
        var selector = new BeatTrackerSelector(builtIn, beatThis);
        var trackingResult = selector.Track(CreateAudio(), CreateFeatures(), CreateShadowOptions());
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = trackingResult.CandidateSet
        };

        var export = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        export.Candidates!.AgreementConfidence.Should().BeSameAs(trackingResult.CandidateSet!.AgreementConfidence);
        export.Candidates.AgreementConfidence!.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
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
