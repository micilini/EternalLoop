using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridCalibrationProfileFactoryTests
{
    [Fact]
    public void Factory_returns_strict_defaults()
    {
        var weak = BeatGridHybridCalibrationProfileFactory.CreateWeakWindowOptions(HybridCalibrationProfile.StrictProduction);
        var correction = BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(HybridCalibrationProfile.StrictProduction);
        var hybrid = BeatGridHybridCalibrationProfileFactory.CreateHybridSelectionOptions(HybridCalibrationProfile.StrictProduction);

        weak.MinWeaknessScore.Should().Be(0.55);
        correction.RequireFutureCorrectionCandidate.Should().BeTrue();
        correction.CalibrationProfile.Should().Be("strict-production");
        hybrid.CalibrationProfileName.Should().Be("strict-production");
    }

    [Fact]
    public void Factory_returns_balanced_relaxed_thresholds()
    {
        var weak = BeatGridHybridCalibrationProfileFactory.CreateWeakWindowOptions(HybridCalibrationProfile.BalancedProbe);
        var correction = BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(HybridCalibrationProfile.BalancedProbe);
        var hybrid = BeatGridHybridCalibrationProfileFactory.CreateHybridSelectionOptions(HybridCalibrationProfile.BalancedProbe);

        weak.MinWeaknessScore.Should().BeLessThan(0.55);
        weak.MinAdvisorAgreementF1_70Ms.Should().BeLessThan(0.70);
        correction.MinCorrectionReadinessScore.Should().BeLessThan(0.72);
        correction.CalibrationProfile.Should().Be("balanced-probe");
        hybrid.MaxCorrectedVsLegacyCountRatioDelta.Should().BeGreaterThan(0.25);
    }

    [Fact]
    public void Factory_returns_exploratory_more_relaxed_than_balanced()
    {
        var balancedWeak = BeatGridHybridCalibrationProfileFactory.CreateWeakWindowOptions(HybridCalibrationProfile.BalancedProbe);
        var exploratoryWeak = BeatGridHybridCalibrationProfileFactory.CreateWeakWindowOptions(HybridCalibrationProfile.ExploratoryProbe);
        var balancedCorrection = BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(HybridCalibrationProfile.BalancedProbe);
        var exploratoryCorrection = BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(HybridCalibrationProfile.ExploratoryProbe);

        exploratoryWeak.MinWeaknessScore.Should().BeLessThan(balancedWeak.MinWeaknessScore);
        exploratoryWeak.MaxAdvisorAbsOffsetMs.Should().BeGreaterThan(balancedWeak.MaxAdvisorAbsOffsetMs);
        exploratoryCorrection.RequireFutureCorrectionCandidate.Should().BeFalse();
        exploratoryCorrection.MinCorrectionReadinessScore.Should().BeLessThan(balancedCorrection.MinCorrectionReadinessScore);
    }
}
