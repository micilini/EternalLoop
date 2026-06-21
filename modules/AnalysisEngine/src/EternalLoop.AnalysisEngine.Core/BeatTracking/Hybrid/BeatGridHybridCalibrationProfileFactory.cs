using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;

public static class BeatGridHybridCalibrationProfileFactory
{
    public static BeatGridWeakWindowOptions CreateWeakWindowOptions(HybridCalibrationProfile profile)
    {
        return profile switch
        {
            HybridCalibrationProfile.StrictProduction => new BeatGridWeakWindowOptions(),

            HybridCalibrationProfile.BalancedProbe => new BeatGridWeakWindowOptions
            {
                MinWeaknessScore = 0.45,
                MinAdvisorStrengthScore = 0.50,
                MinExperimentalCorrectionReadinessScore = 0.60,
                MinAdvisorAgreementF1_70Ms = 0.55,
                StrongAdvisorAgreementF1_70Ms = 0.75,
                MaxAdvisorAbsOffsetMs = 160.0,
                MaxCountRatioDelta = 0.45
            },

            HybridCalibrationProfile.ExploratoryProbe => new BeatGridWeakWindowOptions
            {
                MinWeaknessScore = 0.35,
                MinAdvisorStrengthScore = 0.40,
                MinExperimentalCorrectionReadinessScore = 0.48,
                MinAdvisorAgreementF1_70Ms = 0.45,
                StrongAdvisorAgreementF1_70Ms = 0.65,
                MaxAdvisorAbsOffsetMs = 220.0,
                MaxCountRatioDelta = 0.60
            },

            _ => new BeatGridWeakWindowOptions()
        };
    }

    public static BeatGridWeakWindowCorrectionOptions CreateCorrectionOptions(HybridCalibrationProfile profile)
    {
        return profile switch
        {
            HybridCalibrationProfile.StrictProduction => new BeatGridWeakWindowCorrectionOptions(),

            HybridCalibrationProfile.BalancedProbe => new BeatGridWeakWindowCorrectionOptions
            {
                CalibrationProfile = "balanced-probe",
                MinAdvisorStrengthScore = 0.50,
                MinCorrectionReadinessScore = 0.60,
                MaxAllowedOffsetMs = 160.0,
                MaxAllowedCountRatioDelta = 0.45,
                MaxAllowedBpmDelta = 28.0,
                RequireFutureCorrectionCandidate = true
            },

            HybridCalibrationProfile.ExploratoryProbe => new BeatGridWeakWindowCorrectionOptions
            {
                CalibrationProfile = "exploratory-probe",
                MinAdvisorStrengthScore = 0.40,
                MinCorrectionReadinessScore = 0.48,
                MaxAllowedOffsetMs = 220.0,
                MaxAllowedCountRatioDelta = 0.60,
                MaxAllowedBpmDelta = 40.0,
                RequireFutureCorrectionCandidate = false
            },

            _ => new BeatGridWeakWindowCorrectionOptions()
        };
    }

    public static BeatGridHybridSelectionOptions CreateHybridSelectionOptions(HybridCalibrationProfile profile)
    {
        return profile switch
        {
            HybridCalibrationProfile.StrictProduction => new BeatGridHybridSelectionOptions(),

            HybridCalibrationProfile.BalancedProbe => new BeatGridHybridSelectionOptions
            {
                CalibrationProfileName = "balanced-probe",
                MaxCorrectedVsLegacyCountRatioDelta = 0.35,
                MaxCorrectedBpm = 210.0,
                MaxCorrectedBeatDensityPerSecond = 4.5,
                MinCorrectedMedianIntervalSeconds = 0.22
            },

            HybridCalibrationProfile.ExploratoryProbe => new BeatGridHybridSelectionOptions
            {
                CalibrationProfileName = "exploratory-probe",
                MaxCorrectedVsLegacyCountRatioDelta = 0.50,
                MaxCorrectedBpm = 230.0,
                MaxCorrectedBeatDensityPerSecond = 5.0,
                MinCorrectedMedianIntervalSeconds = 0.18
            },

            _ => new BeatGridHybridSelectionOptions()
        };
    }
}
