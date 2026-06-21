using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;
using EternalLoop.AnalysisEngine.Core.Models;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Models;

public sealed class BeatProviderExportDiagnosticsTests
{
    [Fact]
    public void FromDiagnostics_maps_built_in_provider()
    {
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderVersion = "analysisengine-built-in",
            BeatProviderLicense = "MIT",
            BeatProviderModelName = "none",
            BeatProviderModelSha256 = "none",
            BeatProviderUsedAi = false,
            BeatProviderUsedBuiltIn = true,
            BeatProviderUsedFallback = false,
            BeatGridMode = "composite-dp",
            TatumMode = "uniform-fallback",
            RequestedTatumMode = "Default",
            BarPhaseMode = "phase-zero"
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Name.Should().Be("built-in");
        result.Mode.Should().Be("dsp");
        result.ModelName.Should().Be("none");
        result.UsedAi.Should().BeFalse();
        result.UsedBuiltIn.Should().BeTrue();
        result.UsedFallback.Should().BeFalse();
        result.BeatGridMode.Should().Be("composite-dp");
        result.TatumMode.Should().Be("uniform-fallback");
    }

    [Fact]
    public void FromDiagnostics_maps_ai_provider()
    {
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "beat-this",
            BeatProviderVersion = "1.0",
            BeatProviderLicense = "MIT",
            BeatProviderModelName = "beat-this-large",
            BeatProviderModelSha256 = "abc123",
            BeatProviderUsedAi = true,
            BeatProviderUsedBuiltIn = false,
            BeatProviderUsedFallback = false,
            BeatProviderDownbeatCount = 10,
            BeatProviderBeatNumberCount = 40,
            BeatProviderEstimatedMeter = 4,
            BeatGridMode = "beat-this-onnx-musical-v1",
            TatumMode = "fixed-two-per-beat",
            RequestedTatumMode = "Default",
            BarPhaseMode = "provider-downbeats"
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Name.Should().Be("beat-this");
        result.Mode.Should().Be("onnx-local");
        result.ModelName.Should().Be("beat-this-large");
        result.ModelSha256.Should().Be("abc123");
        result.UsedAi.Should().BeTrue();
        result.UsedBuiltIn.Should().BeFalse();
        result.UsedFallback.Should().BeFalse();
        result.DownbeatCount.Should().Be(10);
        result.BeatNumberCount.Should().Be(40);
        result.EstimatedMeter.Should().Be(4);
        result.TatumMode.Should().Be("fixed-two-per-beat");
        result.BarPhaseMode.Should().Be("provider-downbeats");
    }

    [Fact]
    public void FromDiagnostics_marks_fallback_mode()
    {
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedAi = false,
            BeatProviderUsedBuiltIn = true,
            BeatProviderUsedFallback = true,
            BeatProviderFallbackReason = "beat-this-provider-failed: model missing"
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Mode.Should().Be("fallback");
        result.UsedFallback.Should().BeTrue();
        result.FallbackReason.Should().Be("beat-this-provider-failed: model missing");
    }

    [Fact]
    public void FromDiagnostics_marks_hybrid_experimental_mode()
    {
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "hybrid",
            BeatProviderUsedAi = true,
            BeatProviderUsedBuiltIn = true,
            BeatProviderUsedHybrid = true,
            BeatProviderUsedFallback = false
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Mode.Should().Be("hybrid-experimental");
        result.UsedHybrid.Should().BeTrue();
    }

    [Fact]
    public void FromDiagnostics_marks_hybrid_fallback_mode()
    {
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "hybrid",
            BeatProviderUsedAi = true,
            BeatProviderUsedBuiltIn = true,
            BeatProviderUsedHybrid = true,
            BeatProviderUsedFallback = true
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Mode.Should().Be("hybrid-fallback");
        result.UsedHybrid.Should().BeTrue();
    }

    [Fact]
    public void FromDiagnostics_maps_shadow_diagnostics()
    {
        var shadow = BeatGridShadowDiagnostics.Succeeded(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            new BeatGridShadowComparison { F1_70Ms = 1.0 });
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedAi = false,
            BeatProviderUsedBuiltIn = true,
            BeatProviderUsedFallback = false,
            BeatProviderShadowDiagnostics = shadow
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Shadow.Should().BeSameAs(shadow);
        result.Shadow!.Status.Should().Be("succeeded");
    }

    [Fact]
    public void FromDiagnostics_sets_mode_dsp_shadow_when_shadow_enabled()
    {
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedAi = false,
            BeatProviderUsedBuiltIn = true,
            BeatProviderUsedFallback = false,
            BeatProviderShadowDiagnostics = BeatGridShadowDiagnostics.NotConfigured(CreateBeatTrackingResult("built-in"))
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Mode.Should().Be("dsp-shadow");
    }

    [Fact]
    public void BeatProviderExportDiagnostics_maps_candidate_set()
    {
        var candidateSet = new BeatGridCandidateFactory().CreateShadowSet(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            advisorAvailable: true);
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = candidateSet
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Candidates.Should().BeSameAs(candidateSet);
        result.Candidates!.Diagnostics.SelectedCandidateId.Should().Be("legacy");
    }

    [Fact]
    public void BeatProviderExportDiagnostics_shadow_mode_contains_candidate_diagnostics()
    {
        var candidateSet = new BeatGridCandidateFactory().CreateShadowSet(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            advisorAvailable: true);
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderShadowDiagnostics = BeatGridShadowDiagnostics.NotConfigured(CreateBeatTrackingResult("built-in"), candidateSet),
            BeatProviderCandidateSet = candidateSet
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Mode.Should().Be("dsp-shadow");
        result.Shadow!.CandidateSetEnabled.Should().BeTrue();
        result.Candidates!.Diagnostics.AdvisorCandidateId.Should().Be("beat-this-advisor");
    }

    [Fact]
    public void BeatProviderExportDiagnostics_includes_phase_alignment_under_candidates()
    {
        var phaseAlignment = new BeatGridPhaseAlignmentDiagnostics
        {
            Enabled = true,
            Status = "offset-detected",
            BestOffsetMs = -40.0,
            Confidence = BeatGridPhaseAlignmentConfidence.High,
            ShouldApplyCorrection = false
        };
        var candidateSet = new BeatGridCandidateFactory().CreateShadowSet(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            advisorAvailable: true,
            phaseAlignment: phaseAlignment);
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = candidateSet
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Candidates!.PhaseAlignment.Should().BeSameAs(phaseAlignment);
        result.Candidates.PhaseAlignment!.BestOffsetMs.Should().Be(-40.0);
    }

    [Fact]
    public void Exported_phase_alignment_never_sets_should_apply_correction_true()
    {
        var candidateSet = new BeatGridCandidateFactory().CreateShadowSet(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            advisorAvailable: true,
            phaseAlignment: new BeatGridPhaseAlignmentDiagnostics
            {
                Enabled = true,
                Status = "offset-detected",
                ShouldApplyCorrection = false
            });
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = candidateSet
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Candidates!.PhaseAlignment!.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void BeatProviderExportDiagnostics_includes_agreement_confidence_under_candidates()
    {
        var agreement = new BeatGridAgreementConfidenceDiagnostics
        {
            Enabled = true,
            Status = "evaluated",
            GlobalConfidence = new BeatGridAgreementConfidenceScore
            {
                Level = BeatGridAgreementConfidenceLevel.High,
                Score = 0.85,
                F1_70Ms = 0.90,
                IsReliable = true
            },
            FutureFusionReadiness = "diagnostic-ready",
            ExternalBenchmarkClaimStatus = "not-evaluated"
        };
        var candidateSet = new BeatGridCandidateFactory().CreateShadowSet(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            advisorAvailable: true).WithAgreementConfidence(agreement);
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = candidateSet
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Candidates!.AgreementConfidence.Should().BeSameAs(agreement);
        result.Candidates.AgreementConfidence!.GlobalConfidence!.Level.Should().Be(BeatGridAgreementConfidenceLevel.High);
        result.Candidates.AgreementConfidence.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }

    [Fact]
    public void BeatProviderExportDiagnostics_includes_weak_windows_under_candidates()
    {
        var weakWindows = new BeatGridWeakWindowDiagnostics
        {
            Enabled = true,
            Status = "evaluated",
            WeakWindowCount = 2,
            FutureCorrectionCandidateCount = 1,
            ShouldApplyCorrection = false,
            ExternalBenchmarkClaimStatus = "not-evaluated"
        };
        var candidateSet = new BeatGridCandidateFactory().CreateShadowSet(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            advisorAvailable: true).WithWeakWindows(weakWindows);
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = candidateSet
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Candidates!.WeakWindows.Should().BeSameAs(weakWindows);
        result.Candidates.WeakWindows!.ShouldApplyCorrection.Should().BeFalse();
        result.Candidates.WeakWindows.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }

    [Fact]
    public void BeatProviderExportDiagnostics_includes_weak_window_corrections_under_candidates()
    {
        var corrected = new BeatGridCandidate
        {
            Id = "weak-window-corrected-experimental",
            Source = BeatGridCandidateSourceKind.WeakWindowCorrectedExperimental,
            Role = BeatGridCandidateRole.CorrectedExperimental,
            ProviderName = "hybrid-experimental",
            BeatTimes = [0.0, 0.5],
            Quality = new BeatGridCandidateQuality { BeatCount = 2, IsPlausible = true }
        };
        var correction = new BeatGridWeakWindowCorrectionResult
        {
            CorrectedCandidate = corrected,
            Plan = new BeatGridWeakWindowCorrectionPlan { Enabled = true, Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate },
            Diagnostics = new BeatGridWeakWindowCorrectionDiagnostics
            {
                Enabled = true,
                Status = "candidate-created",
                Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
                CorrectedCandidateCreated = true,
                CorrectedCandidateId = corrected.Id,
                ExternalBenchmarkClaimStatus = "not-evaluated"
            }
        };
        var candidateSet = new BeatGridCandidateFactory().CreateShadowSet(
            CreateBeatTrackingResult("built-in"),
            CreateBeatTrackingResult("beat-this"),
            advisorAvailable: true).WithWeakWindowCorrection(correction);
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "built-in",
            BeatProviderUsedBuiltIn = true,
            BeatProviderCandidateSet = candidateSet
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Candidates!.CorrectedExperimental.Should().BeSameAs(corrected);
        result.Candidates.WeakWindowCorrections!.CorrectedCandidateCreated.Should().BeTrue();
        result.Candidates.WeakWindowCorrections.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void BeatProviderExportDiagnostics_includes_hybrid_selection_under_candidates()
    {
        var set = BeatTracking.Hybrid.BeatGridHybridSafetyGateTests.CreateSet();
        var (selected, hybridSelection) = new Core.BeatTracking.Hybrid.BeatGridHybridSelector().SelectExplicitHybrid(set);
        set = set.WithHybridSelection(selected, hybridSelection);
        var diagnostics = new AnalysisDiagnostics
        {
            BeatProviderName = "hybrid",
            BeatProviderUsedBuiltIn = true,
            BeatProviderUsedHybrid = true,
            BeatProviderCandidateSet = set
        };

        var result = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics);

        result.Candidates!.HybridSelection.Should().BeSameAs(hybridSelection);
        result.Candidates.HybridSelection!.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }

    [Fact]
    public void FromDiagnostics_with_null_returns_built_in_defaults()
    {
        var result = BeatProviderExportDiagnostics.FromDiagnostics(null);

        result.Name.Should().Be("built-in");
        result.Mode.Should().Be("dsp");
        result.UsedBuiltIn.Should().BeTrue();
        result.UsedAi.Should().BeFalse();
    }

    private static BeatTrackingResult CreateBeatTrackingResult(string provider)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            ProviderName = provider
        };
    }
}
