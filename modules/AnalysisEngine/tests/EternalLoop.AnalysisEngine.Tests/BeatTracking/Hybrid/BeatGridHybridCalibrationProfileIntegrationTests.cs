using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridCalibrationProfileIntegrationTests
{
    [Fact]
    public void Hybrid_strict_profile_preserves_existing_behavior()
    {
        var candidateSet = CreateCandidateSet(bestF1: 0.95);
        var strictOptions = BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(HybridCalibrationProfile.StrictProduction);
        var defaultOptions = new BeatGridWeakWindowCorrectionOptions();

        strictOptions.RequireFutureCorrectionCandidate.Should().Be(defaultOptions.RequireFutureCorrectionCandidate);
        strictOptions.MinAdvisorStrengthScore.Should().Be(defaultOptions.MinAdvisorStrengthScore);
        strictOptions.MinCorrectionReadinessScore.Should().Be(defaultOptions.MinCorrectionReadinessScore);
        strictOptions.MaxAllowedOffsetMs.Should().Be(defaultOptions.MaxAllowedOffsetMs);
        strictOptions.MaxAllowedCountRatioDelta.Should().Be(defaultOptions.MaxAllowedCountRatioDelta);

        var strict = new BeatGridWeakWindowCorrectionAnalyzer(strictOptions).Analyze(candidateSet);
        var baseline = new BeatGridWeakWindowCorrectionAnalyzer(defaultOptions).Analyze(candidateSet);

        strict.Diagnostics.Status.Should().Be(baseline.Diagnostics.Status);
        strict.Diagnostics.CandidateWindowCount.Should().Be(baseline.Diagnostics.CandidateWindowCount);
        strict.Diagnostics.AcceptedWindowCount.Should().Be(baseline.Diagnostics.AcceptedWindowCount);
        strict.Diagnostics.CalibrationProfile.Should().Be("strict-production");
    }

    [Fact]
    public void Hybrid_balanced_profile_can_create_more_future_candidates_than_strict_on_fixture()
    {
        var candidateSet = CreateCandidateSet(bestF1: 0.60, irregularitySeconds: 0.22);
        var strict = new BeatGridWeakWindowAnalyzer(
            BeatGridHybridCalibrationProfileFactory.CreateWeakWindowOptions(HybridCalibrationProfile.StrictProduction))
            .Analyze(candidateSet);
        var balanced = new BeatGridWeakWindowAnalyzer(
            BeatGridHybridCalibrationProfileFactory.CreateWeakWindowOptions(HybridCalibrationProfile.BalancedProbe))
            .Analyze(candidateSet);

        balanced.FutureCorrectionCandidateCount.Should().BeGreaterThan(strict.FutureCorrectionCandidateCount);
        balanced.FutureCorrectionCandidateCount.Should().BeGreaterThan(0);
        balanced.ShouldModifyFinalGrid.Should().BeFalse();
    }

    [Fact]
    public void Hybrid_exploratory_profile_can_disable_require_future_candidate_only_in_probe()
    {
        var window = CreateWeakWindow(futureCorrectionCandidate: false);
        var candidateSet = CreateCandidateSetFromWeakWindow(window);
        var strict = new BeatGridWeakWindowCorrectionAnalyzer(
            BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(HybridCalibrationProfile.StrictProduction))
            .Analyze(candidateSet);
        var exploratory = new BeatGridWeakWindowCorrectionAnalyzer(
            BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(HybridCalibrationProfile.ExploratoryProbe))
            .Analyze(candidateSet);

        strict.CorrectedCandidate.Should().BeNull();
        strict.Diagnostics.CandidateWindowCount.Should().Be(0);
        exploratory.Diagnostics.CandidateWindowCount.Should().Be(1);
        exploratory.Diagnostics.CalibrationProfile.Should().Be("exploratory-probe");
    }

    [Fact]
    public void Auto_ignores_hybrid_calibration_profile()
    {
        var builtIn = new RecordingBeatTracker(CreateResult("built-in", usedBuiltIn: true));
        var beatThis = new ThrowingBeatTracker();
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Auto,
            HybridCalibrationProfile = HybridCalibrationProfile.ExploratoryProbe
        });

        result.Should().BeSameAs(builtIn.Result);
        builtIn.CallCount.Should().Be(1);
        beatThis.CallCount.Should().Be(0);
        result.CandidateSet.Should().BeNull();
    }

    private static BeatGridCandidateSet CreateCandidateSet(double bestF1, double irregularitySeconds = 0.40)
    {
        var legacy = GenerateRegularBeats(80);
        legacy[18] += irregularitySeconds;
        legacy[19] -= irregularitySeconds / 2.0;
        legacy[20] += irregularitySeconds * 0.75;

        var legacyCandidate = CreateCandidate("legacy", legacy);
        var advisorCandidate = CreateCandidate("beat-this-advisor", GenerateRegularBeats(80));

        return new BeatGridCandidateSet
        {
            Legacy = legacyCandidate,
            Advisor = advisorCandidate,
            Selected = legacyCandidate,
            All = [legacyCandidate, advisorCandidate],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true, SelectedCandidateId = "legacy" },
            PhaseAlignment = CreatePhaseAlignment(bestF1),
            AgreementConfidence = CreateAgreement(bestF1)
        };
    }

    private static BeatGridCandidateSet CreateCandidateSetFromWeakWindow(BeatGridWeakWindow window)
    {
        var legacy = CreateCandidate("legacy", GenerateRegularBeats(80));
        var advisor = CreateCandidate("beat-this-advisor", GenerateRegularBeats(80));

        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            Advisor = advisor,
            Selected = legacy,
            All = [legacy, advisor],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true, SelectedCandidateId = "legacy" },
            PhaseAlignment = CreatePhaseAlignment(0.95),
            AgreementConfidence = CreateAgreement(0.95),
            WeakWindows = new BeatGridWeakWindowDiagnostics
            {
                Enabled = true,
                Status = "evaluated",
                WindowCount = 1,
                WeakWindowCount = 1,
                PromisingAdvisorWindowCount = 1,
                FutureCorrectionCandidateCount = window.FutureCorrectionCandidate ? 1 : 0,
                FutureCorrectionReadiness = "diagnostic-ready",
                Windows = [window]
            }
        };
    }

    private static BeatGridWeakWindow CreateWeakWindow(bool futureCorrectionCandidate)
    {
        return new BeatGridWeakWindow
        {
            Index = 0,
            StartTimeSeconds = 8.0,
            EndTimeSeconds = 16.0,
            LegacyBeatCount = 16,
            AdvisorBeatCount = 16,
            IsWeakWindow = true,
            AdvisorIsPromising = true,
            FutureCorrectionCandidate = futureCorrectionCandidate,
            RiskLevel = BeatGridWeakWindowRiskLevel.Low,
            CorrectionReadiness = BeatGridWeakWindowCorrectionReadiness.CandidateForExperimentalCorrection,
            AdvisorStrength = BeatGridWeakWindowCandidateStrength.Moderate,
            Metrics = new BeatGridWeakWindowLocalMetrics
            {
                LegacyIntervalCv = 0.20,
                AdvisorIntervalCv = 0.02,
                LegacyMedianIntervalSeconds = 0.50,
                AdvisorMedianIntervalSeconds = 0.50,
                LegacyBeatDensityPerSecond = 2.0,
                AdvisorBeatDensityPerSecond = 2.0,
                LocalCountRatio = 1.0,
                LocalBestOffsetMs = 0.0,
                LocalZeroOffsetF1_70Ms = 0.95,
                LocalBestOffsetF1_70Ms = 0.95,
                WeaknessScore = 0.55,
                AdvisorStrengthScore = 0.50,
                CorrectionReadinessScore = 0.50
            }
        };
    }

    private static BeatGridCandidate CreateCandidate(string id, double[] beats)
    {
        return new BeatGridCandidate
        {
            Id = id,
            Source = id == "legacy" ? BeatGridCandidateSourceKind.LegacyBuiltIn : BeatGridCandidateSourceKind.BeatThisAdvisor,
            Role = id == "legacy" ? BeatGridCandidateRole.SafeAuthority : BeatGridCandidateRole.Advisor,
            ProviderName = id == "legacy" ? "built-in" : "beat-this",
            BeatTimes = beats,
            EstimatedBpm = 120.0,
            Quality = new BeatGridCandidateQuality
            {
                BeatCount = beats.Length,
                IsDenseGrid = false,
                IsPlausible = true
            }
        };
    }

    private static double[] GenerateRegularBeats(int count)
    {
        return Enumerable.Range(0, count).Select(index => index * 0.5).ToArray();
    }

    private static BeatGridPhaseAlignmentDiagnostics CreatePhaseAlignment(double f1)
    {
        return new BeatGridPhaseAlignmentDiagnostics
        {
            Enabled = true,
            Status = "aligned",
            CountRatio = 1.0,
            BestOffsetMs = 0.0,
            BestOffset = new BeatGridPhaseAlignmentMetrics { F1_70Ms = f1 },
            IsOffsetStable = true,
            Confidence = BeatGridPhaseAlignmentConfidence.High,
            Windows = Enumerable.Range(0, 4).Select(index => new BeatGridPhaseAlignmentWindow
            {
                Index = index,
                StartTimeSeconds = index * 8.0,
                EndTimeSeconds = (index * 8.0) + 15.5,
                LegacyBeatCount = 32,
                AdvisorBeatCount = 32,
                BestOffsetMs = 0.0,
                ZeroOffsetF1_70Ms = f1,
                BestOffsetF1_70Ms = f1,
                IsReliable = true
            }).ToArray()
        };
    }

    private static BeatGridAgreementConfidenceDiagnostics CreateAgreement(double f1)
    {
        return new BeatGridAgreementConfidenceDiagnostics
        {
            Enabled = true,
            Status = "evaluated",
            GlobalConfidence = new BeatGridAgreementConfidenceScore
            {
                Level = BeatGridAgreementConfidenceLevel.High,
                Score = f1,
                F1_70Ms = f1,
                IsReliable = true
            },
            Windows = Enumerable.Range(0, 4).Select(index => new BeatGridAgreementConfidenceWindow
            {
                Index = index,
                StartTimeSeconds = index * 8.0,
                EndTimeSeconds = (index * 8.0) + 15.5,
                BestOffsetMs = 0.0,
                BestOffsetF1_70Ms = f1,
                Confidence = new BeatGridAgreementConfidenceScore
                {
                    Level = BeatGridAgreementConfidenceLevel.High,
                    Score = f1,
                    F1_70Ms = f1,
                    IsReliable = true
                },
                FutureFusionCandidate = true
            }).ToArray()
        };
    }

    private static BeatTrackingResult CreateResult(string providerName, bool usedBuiltIn)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = GenerateRegularBeats(16),
            Confidences = Enumerable.Repeat(0.9, 16).ToArray(),
            ProviderName = providerName,
            UsedBuiltInProvider = usedBuiltIn,
            UsedAiProvider = !usedBuiltIn,
            BeatGridMode = providerName
        };
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

        public int CallCount { get; private set; }

        public BeatTrackingResult Track(LoadedAudio audio, FeatureMatrix features, BeatTrackingOptions options)
        {
            CallCount++;
            return Result;
        }
    }

    private sealed class ThrowingBeatTracker : IBeatTracker
    {
        public int CallCount { get; private set; }

        public BeatTrackingResult Track(LoadedAudio audio, FeatureMatrix features, BeatTrackingOptions options)
        {
            CallCount++;
            throw new InvalidOperationException("beat-this should not run");
        }
    }
}
