using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Tests.BeatTracking.Correction;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridIntegrationTests
{
    [Fact]
    public void Hybrid_provider_returns_corrected_candidate_as_final_when_safe()
    {
        var result = CreateSafeHybridResult();

        result.BeatTimes.Should().Equal(result.CandidateSet!.CorrectedExperimental!.BeatTimes);
    }

    [Fact]
    public void Hybrid_provider_falls_back_to_legacy_when_corrected_missing()
    {
        var legacy = BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats();
        var selector = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(legacy, legacy);

        var result = selector.Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), HybridOptions());

        result.BeatTimes.Should().Equal(legacy);
        result.UsedFallbackProvider.Should().BeTrue();
        result.BeatGridMode.Should().Be("hybrid-fallback-legacy");
    }

    [Fact]
    public void Hybrid_provider_sets_candidate_set_selected_to_corrected_when_safe()
    {
        var result = CreateSafeHybridResult();

        result.CandidateSet!.Selected.Should().BeSameAs(result.CandidateSet.CorrectedExperimental);
    }

    [Fact]
    public void Hybrid_provider_exports_hybrid_selection()
    {
        CreateSafeHybridResult().CandidateSet!.HybridSelection.Should().NotBeNull();
    }

    [Fact]
    public void Hybrid_provider_sets_used_hybrid_true()
    {
        CreateSafeHybridResult().UsedHybridProvider.Should().BeTrue();
    }

    [Fact]
    public void Hybrid_provider_sets_mode_hybrid_experimental()
    {
        CreateSafeHybridResult().BeatGridMode.Should().Be("hybrid-experimental");
    }

    [Fact]
    public void Hybrid_fallback_sets_mode_hybrid_fallback()
    {
        var selector = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats());

        var result = selector.Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), HybridOptions());

        result.BeatGridMode.Should().Be("hybrid-fallback-legacy");
    }

    [Fact]
    public void Auto_does_not_call_hybrid()
    {
        var result = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats())
            .Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.Auto });

        result.UsedHybridProvider.Should().BeFalse();
        result.CandidateSet.Should().BeNull();
    }

    [Fact]
    public void Shadow_does_not_select_corrected_candidate()
    {
        var result = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats())
            .Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.Shadow });

        result.CandidateSet!.CorrectedExperimental.Should().NotBeNull();
        result.CandidateSet.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
        result.UsedHybridProvider.Should().BeFalse();
    }

    [Fact]
    public void BuiltIn_does_not_call_hybrid()
    {
        var result = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats())
            .Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.BuiltIn });

        result.UsedHybridProvider.Should().BeFalse();
        result.CandidateSet.Should().BeNull();
    }

    private static BeatTrackingResult CreateSafeHybridResult()
    {
        return BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats())
            .Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), HybridOptions());
    }

    private static BeatTrackingOptions HybridOptions()
    {
        return new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.Hybrid };
    }
}
