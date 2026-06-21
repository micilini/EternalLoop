using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionAnalyzerTests
{
    [Fact]
    public void Analyze_without_weak_windows_returns_not_available()
    {
        var set = CreateSet().WithAgreementConfidence(BeatGridAgreementConfidenceDiagnostics.NotAvailable("x"));

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.CorrectedCandidate.Should().BeNull();
        result.Diagnostics.Status.Should().Be("not-available");
    }

    [Fact]
    public void Analyze_without_advisor_returns_not_available()
    {
        var legacy = CreateCandidate("legacy", CreateLegacy());
        var set = new BeatGridCandidateSet
        {
            Legacy = legacy,
            Selected = legacy,
            All = [legacy],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true },
            WeakWindows = BeatGridWeakWindowDiagnostics.NotAvailable("advisor-not-available"),
            PhaseAlignment = BeatGridPhaseAlignmentDiagnostics.NotAvailable("advisor-not-available"),
            AgreementConfidence = BeatGridAgreementConfidenceDiagnostics.NotAvailable("advisor-not-available")
        };

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.CorrectedCandidate.Should().BeNull();
        result.Diagnostics.RejectionReason.Should().Be("advisor-not-available");
    }

    [Fact]
    public void Analyze_disabled_mode_creates_no_candidate()
    {
        var analyzer = new BeatGridWeakWindowCorrectionAnalyzer(new BeatGridWeakWindowCorrectionOptions { Mode = BeatGridWeakWindowCorrectionMode.Disabled });

        var result = analyzer.Analyze(CreateReadySet());

        result.CorrectedCandidate.Should().BeNull();
        result.Diagnostics.Status.Should().Be("disabled");
    }

    [Fact]
    public void Analyze_diagnostics_only_mode_creates_plan_but_no_candidate()
    {
        var analyzer = new BeatGridWeakWindowCorrectionAnalyzer(new BeatGridWeakWindowCorrectionOptions { Mode = BeatGridWeakWindowCorrectionMode.DiagnosticsOnly });

        var result = analyzer.Analyze(CreateReadySet());

        result.CorrectedCandidate.Should().BeNull();
        result.Plan.CandidateWindowCount.Should().Be(1);
        result.Diagnostics.Status.Should().Be("diagnostics-only");
    }

    [Fact]
    public void Analyze_experimental_mode_creates_corrected_candidate_for_ready_window()
    {
        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(CreateReadySet());

        result.CorrectedCandidate.Should().NotBeNull();
        result.CorrectedCandidate!.Id.Should().Be("weak-window-corrected-experimental");
        result.CorrectedCandidate.Role.Should().Be(BeatGridCandidateRole.CorrectedExperimental);
        result.Diagnostics.CorrectedCandidateCreated.Should().BeTrue();
    }

    [Fact]
    public void Analyze_replaces_only_inside_accepted_window()
    {
        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(CreateReadySet());

        result.CorrectedCandidate!.BeatTimes.Should().Contain(2.0);
        result.CorrectedCandidate.BeatTimes.Should().Contain(2.5);
        result.CorrectedCandidate.BeatTimes.Should().Contain(3.0);
        result.CorrectedCandidate.BeatTimes.Should().Contain(0.0);
        result.CorrectedCandidate.BeatTimes.Should().Contain(4.3);
        result.CorrectedCandidate.BeatTimes.Should().NotContain(2.3);
    }

    [Fact]
    public void Analyze_keeps_legacy_outside_weak_windows()
    {
        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(CreateReadySet());

        result.CorrectedCandidate!.BeatTimes.Where(beat => beat < 1.8 || beat > 3.6)
            .Should().Equal(CreateLegacy().Where(beat => beat < 1.8 || beat > 3.6));
    }

    [Fact]
    public void Analyze_rejects_high_risk_window()
    {
        var set = CreateReadySet(risk: BeatGridWeakWindowRiskLevel.High);

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.CorrectedCandidate.Should().BeNull();
        result.Plan.RejectedWindowCount.Should().Be(0);
        result.Diagnostics.Status.Should().Be("rejected");
    }

    [Fact]
    public void Analyze_rejects_bad_count_ratio()
    {
        var set = CreateReadySet(countRatio: 1.5);

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.CorrectedCandidate.Should().BeNull();
        result.Diagnostics.RejectionReason.Should().Be("no-correction-windows-accepted");
    }

    [Fact]
    public void Analyze_when_no_candidate_windows_reports_blocker_counts()
    {
        var set = CreateReadySet(countRatio: 1.5);

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.Diagnostics.BlockerCounts.Should().ContainKey("bad-count-ratio");
        result.Diagnostics.BlockerCounts["bad-count-ratio"].Should().Be(1);
    }

    [Fact]
    public void Analyze_when_no_candidate_windows_reports_top_blockers()
    {
        var set = CreateReadySet(countRatio: 1.5);

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.Diagnostics.TopBlockers.Should().ContainSingle().Which.Should().Be("bad-count-ratio");
    }

    [Fact]
    public void Analyze_when_candidate_windows_exist_reports_candidate_window_count()
    {
        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(CreateReadySet(twoWindows: true));

        result.Diagnostics.CandidateWindowCount.Should().Be(2);
        result.Diagnostics.WeakWindowCount.Should().Be(2);
        result.Diagnostics.FutureCorrectionCandidateCount.Should().Be(2);
    }

    [Fact]
    public void Analyze_when_accepted_windows_zero_reports_no_correction_windows_accepted()
    {
        var set = CreateReadySet(risk: BeatGridWeakWindowRiskLevel.High);

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.Diagnostics.AcceptedWindowCount.Should().Be(0);
        result.Diagnostics.RejectionReason.Should().Be("no-correction-windows-accepted");
    }

    [Fact]
    public void Analyze_does_not_create_corrected_candidate_from_non_candidate_windows()
    {
        var set = CreateReadySet(countRatio: 1.5);

        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);

        result.CorrectedCandidate.Should().BeNull();
        result.Diagnostics.CorrectedCandidateCreated.Should().BeFalse();
        result.Diagnostics.CandidateWindowCount.Should().Be(0);
    }

    [Fact]
    public void Analyze_rejects_dense_corrected_candidate()
    {
        var analyzer = new BeatGridWeakWindowCorrectionAnalyzer(new BeatGridWeakWindowCorrectionOptions { MaxCorrectedBeatDensityPerSecond = 1.0 });

        var result = analyzer.Analyze(CreateReadySet());

        result.CorrectedCandidate.Should().BeNull();
        result.Diagnostics.RejectionReason.Should().Be("corrected-density-too-high");
    }

    [Fact]
    public void Analyze_limits_max_windows_to_correct()
    {
        var analyzer = new BeatGridWeakWindowCorrectionAnalyzer(new BeatGridWeakWindowCorrectionOptions { MaxWindowsToCorrect = 1 });
        var set = CreateReadySet(twoWindows: true);

        var result = analyzer.Analyze(set);

        result.Plan.CandidateWindowCount.Should().Be(1);
    }

    [Fact]
    public void Analyze_corrected_candidate_is_not_selected_final()
    {
        var set = CreateReadySet();
        var result = new BeatGridWeakWindowCorrectionAnalyzer().Analyze(set);
        var updated = set.WithWeakWindowCorrection(result);

        updated.Selected.Should().BeSameAs(set.Selected);
        updated.CorrectedExperimental.Should().NotBeSameAs(updated.Selected);
    }

    [Fact]
    public void Analyze_never_modifies_final_grid()
    {
        new BeatGridWeakWindowCorrectionAnalyzer().Analyze(CreateReadySet()).Diagnostics.ShouldModifyFinalGrid.Should().BeFalse();
    }

    [Fact]
    public void Analyze_never_claims_madmom_forever()
    {
        new BeatGridWeakWindowCorrectionAnalyzer().Analyze(CreateReadySet()).Diagnostics.ExternalBenchmarkClaimStatus.Should().Be("not-evaluated");
    }

    public static BeatGridCandidateSet CreateReadySet(
        BeatGridWeakWindowRiskLevel risk = BeatGridWeakWindowRiskLevel.Low,
        double countRatio = 1.0,
        bool twoWindows = false)
    {
        var legacy = CreateCandidate("legacy", CreateLegacy());
        var advisor = CreateCandidate("beat-this-advisor", CreateAdvisor());
        var windows = new List<BeatGridWeakWindow> { CreateWeakWindow(0, 1.8, 3.6, risk, countRatio) };
        if (twoWindows)
        {
            windows.Add(CreateWeakWindow(1, 3.6, 4.1, risk, countRatio));
        }

        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            Advisor = advisor,
            Selected = legacy,
            All = [legacy, advisor],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true, SelectedCandidateId = "legacy" },
            PhaseAlignment = new BeatGridPhaseAlignmentDiagnostics { Enabled = true, Status = "aligned" },
            AgreementConfidence = new BeatGridAgreementConfidenceDiagnostics { Enabled = true, Status = "evaluated" },
            WeakWindows = new BeatGridWeakWindowDiagnostics
            {
                Enabled = true,
                Status = "evaluated",
                WindowCount = windows.Count,
                WeakWindowCount = windows.Count,
                FutureCorrectionCandidateCount = risk is BeatGridWeakWindowRiskLevel.Low or BeatGridWeakWindowRiskLevel.Medium && countRatio <= 1.25 ? windows.Count : 0,
                Windows = windows
            }
        };
    }

    private static BeatGridWeakWindow CreateWeakWindow(int index, double start, double end, BeatGridWeakWindowRiskLevel risk, double countRatio)
    {
        var futureCandidate = risk is BeatGridWeakWindowRiskLevel.Low or BeatGridWeakWindowRiskLevel.Medium && Math.Abs(countRatio - 1.0) <= 0.25;
        return new BeatGridWeakWindow
        {
            Index = index,
            StartTimeSeconds = start,
            EndTimeSeconds = end,
            IsWeakWindow = true,
            AdvisorIsPromising = true,
            FutureCorrectionCandidate = futureCandidate,
            RiskLevel = risk,
            CorrectionReadiness = futureCandidate ? BeatGridWeakWindowCorrectionReadiness.CandidateForExperimentalCorrection : BeatGridWeakWindowCorrectionReadiness.Blocked,
            AdvisorStrength = BeatGridWeakWindowCandidateStrength.Strong,
            Metrics = new BeatGridWeakWindowLocalMetrics
            {
                LocalCountRatio = countRatio,
                LocalBestOffsetMs = 0.0,
                AdvisorStrengthScore = 0.9,
                CorrectionReadinessScore = 0.9,
                LocalBestOffsetF1_70Ms = 0.95
            }
        };
    }

    private static BeatGridCandidateSet CreateSet()
    {
        var legacy = CreateCandidate("legacy", CreateLegacy());
        var advisor = CreateCandidate("beat-this-advisor", CreateAdvisor());
        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            Advisor = advisor,
            Selected = legacy,
            All = [legacy, advisor],
            Diagnostics = new BeatGridCandidateDiagnostics { Enabled = true },
            PhaseAlignment = new BeatGridPhaseAlignmentDiagnostics { Enabled = true },
            AgreementConfidence = new BeatGridAgreementConfidenceDiagnostics { Enabled = true }
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
            Quality = new BeatGridCandidateQuality { BeatCount = beats.Length, IsPlausible = true }
        };
    }

    private static double[] CreateLegacy() => [0.0, 0.5, 1.0, 1.5, 2.3, 2.8, 3.3, 3.8, 4.3];

    private static double[] CreateAdvisor() => [0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0];
}
