using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionSafetyTests
{
    [Fact]
    public void Correction_does_not_run_for_auto()
    {
        var selector = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats());

        var result = selector.Track(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(),
            new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.Auto });

        result.CandidateSet.Should().BeNull();
    }

    [Fact]
    public void Correction_does_not_run_for_built_in()
    {
        var selector = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats());

        var result = selector.Track(
            BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(),
            BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(),
            new BeatTrackingOptions { BeatProvider = BeatTrackingProviderKind.BuiltIn });

        result.CandidateSet.Should().BeNull();
    }

    [Fact]
    public void Correction_does_not_change_shadow_final_beat_times()
    {
        var legacy = BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats();
        var result = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(legacy, BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats())
            .Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), BeatGridWeakWindowCorrectionIntegrationTests.ShadowOptions());

        result.BeatTimes.Should().Equal(legacy);
    }

    [Fact]
    public void Correction_does_not_change_candidate_set_selected()
    {
        var result = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
                BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
                BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats())
            .Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), BeatGridWeakWindowCorrectionIntegrationTests.ShadowOptions());

        result.CandidateSet!.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
    }

    [Fact]
    public void Correction_safety_flags_are_always_false()
    {
        var result = BeatGridWeakWindowCorrectionIntegrationTests.CreateSelector(
                BeatGridWeakWindowCorrectionIntegrationTests.CreateIrregularLegacyBeats(),
                BeatGridWeakWindowCorrectionIntegrationTests.CreateRegularBeats())
            .Track(BeatGridWeakWindowCorrectionIntegrationTests.CreateAudio(), BeatGridWeakWindowCorrectionIntegrationTests.CreateFeatures(), BeatGridWeakWindowCorrectionIntegrationTests.ShadowOptions());

        result.CandidateSet!.WeakWindowCorrections!.ShouldModifyFinalGrid.Should().BeFalse();
        result.CandidateSet.WeakWindowCorrections.ShouldSelectCorrectedCandidate.Should().BeFalse();
        result.CandidateSet.WeakWindowCorrections.ShouldApplyCorrection.Should().BeFalse();
    }
}
