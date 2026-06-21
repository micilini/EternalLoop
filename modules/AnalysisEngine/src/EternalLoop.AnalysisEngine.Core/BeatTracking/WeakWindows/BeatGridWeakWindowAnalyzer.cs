using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

public sealed class BeatGridWeakWindowAnalyzer
{
    private readonly BeatGridWeakWindowOptions _options;

    public BeatGridWeakWindowAnalyzer(BeatGridWeakWindowOptions? options = null)
    {
        _options = options ?? new BeatGridWeakWindowOptions();
        _options.Validate();
    }

    public BeatGridWeakWindowDiagnostics Analyze(BeatGridCandidateSet? candidateSet)
    {
        if (candidateSet?.Legacy is null) return BeatGridWeakWindowDiagnostics.NotAvailable("legacy-candidate-missing");
        if (candidateSet.Advisor is null) return BeatGridWeakWindowDiagnostics.NotAvailable("advisor-not-available");
        if (candidateSet.PhaseAlignment is null) return BeatGridWeakWindowDiagnostics.NotAvailable("phase-alignment-missing");
        if (candidateSet.AgreementConfidence is null) return BeatGridWeakWindowDiagnostics.NotAvailable("agreement-confidence-missing");
        if (candidateSet.Advisor.Quality.IsDenseGrid || !candidateSet.Advisor.Quality.IsPlausible)
        {
            return BeatGridWeakWindowDiagnostics.NotAvailable(candidateSet.Advisor.Quality.RejectionReason ?? "advisor-rejected-or-dense");
        }

        var windows = BuildWindows(candidateSet).ToArray();
        var weakCount = windows.Count(window => window.IsWeakWindow);
        var promisingCount = windows.Count(window => window.AdvisorIsPromising);
        var candidateCount = windows.Count(window => window.FutureCorrectionCandidate);
        var blockedCount = windows.Count(window => window.RiskLevel == BeatGridWeakWindowRiskLevel.Blocked);
        var readiness = ResolveGlobalReadiness(candidateCount, weakCount, blockedCount);

        return new BeatGridWeakWindowDiagnostics
        {
            Enabled = true,
            Status = windows.Length == 0 ? "not-available" : "evaluated",
            WindowCount = windows.Length,
            WeakWindowCount = weakCount,
            PromisingAdvisorWindowCount = promisingCount,
            FutureCorrectionCandidateCount = candidateCount,
            BlockedWindowCount = blockedCount,
            FutureCorrectionReadiness = readiness,
            FutureCorrectionReady = readiness == "candidate-ready",
            ShouldModifyFinalGrid = false,
            ShouldSelectAdvisor = false,
            ShouldApplyCorrection = false,
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Windows = windows,
            UnreliableReason = windows.Length == 0 ? "insufficient-evidence" : null,
            Notes = ["Weak-window detection is diagnostic only; final beat grid remains Legacy/BuiltIn."]
        };
    }

    private IEnumerable<BeatGridWeakWindow> BuildWindows(BeatGridCandidateSet candidateSet)
    {
        var legacy = candidateSet.Legacy!.BeatTimes;
        var advisor = candidateSet.Advisor!.BeatTimes;

        for (var startIndex = 0; startIndex + _options.MinWindowBeats <= legacy.Length; startIndex += _options.WindowHopBeatCount)
        {
            var endIndex = Math.Min(startIndex + _options.WindowBeatCount, legacy.Length);
            var legacyWindow = legacy[startIndex..endIndex];
            if (legacyWindow.Length < _options.MinWindowBeats)
            {
                continue;
            }

            var start = legacyWindow[0];
            var end = legacyWindow[^1];
            var advisorWindow = advisor.Where(beat => beat >= start - 0.5 && beat <= end + 0.5).ToArray();
            var phaseWindow = FindBestPhaseWindow(candidateSet.PhaseAlignment!.Windows, start, end);
            var agreementWindow = FindBestAgreementWindow(candidateSet.AgreementConfidence!.Windows, start, end);

            yield return AnalyzeWindow(startIndex / _options.WindowHopBeatCount, start, end, legacyWindow, advisorWindow, phaseWindow, agreementWindow);
        }
    }

    private BeatGridWeakWindow AnalyzeWindow(
        int index,
        double start,
        double end,
        double[] legacyWindow,
        double[] advisorWindow,
        BeatGridPhaseAlignmentWindow? phaseWindow,
        BeatGridAgreementConfidenceWindow? agreementWindow)
    {
        var legacyCv = CalculateIntervalCv(legacyWindow);
        var advisorCv = CalculateIntervalCv(advisorWindow);
        var legacyMedian = CalculateMedianInterval(legacyWindow);
        var advisorMedian = CalculateMedianInterval(advisorWindow);
        var duration = Math.Max(0.001, end - start);
        var localCountRatio = legacyWindow.Length > 0 ? advisorWindow.Length / (double)legacyWindow.Length : (double?)null;
        var bestF1 = phaseWindow?.BestOffsetF1_70Ms ?? 0.0;
        var zeroF1 = phaseWindow?.ZeroOffsetF1_70Ms ?? 0.0;
        var offsetMs = phaseWindow?.BestOffsetMs;
        var agreementScore = agreementWindow?.Confidence?.Score;
        var weaknessScore = CalculateWeaknessScore(legacyCv, legacyMedian, legacyWindow.Length / duration, bestF1);
        var advisorStrengthScore = CalculateAdvisorStrengthScore(advisorCv, bestF1, offsetMs, localCountRatio);
        var readinessScore = (weaknessScore * 0.50) + (advisorStrengthScore * 0.50);
        var reasons = ResolveReasons(legacyCv, advisorCv, legacyMedian, bestF1, localCountRatio, phaseWindow).ToArray();
        var risk = ResolveRisk(bestF1, offsetMs, localCountRatio, phaseWindow);
        var isWeak = weaknessScore >= _options.MinWeaknessScore;
        var advisorPromising = advisorStrengthScore >= _options.MinAdvisorStrengthScore && risk != BeatGridWeakWindowRiskLevel.Blocked;
        var futureCandidate = isWeak
            && advisorPromising
            && readinessScore >= _options.MinExperimentalCorrectionReadinessScore
            && risk is BeatGridWeakWindowRiskLevel.Low or BeatGridWeakWindowRiskLevel.Medium;

        return new BeatGridWeakWindow
        {
            Index = index,
            StartTimeSeconds = start,
            EndTimeSeconds = end,
            LegacyBeatCount = legacyWindow.Length,
            AdvisorBeatCount = advisorWindow.Length,
            IsWeakWindow = isWeak,
            AdvisorIsPromising = advisorPromising,
            FutureCorrectionCandidate = futureCandidate,
            RiskLevel = risk,
            CorrectionReadiness = ResolveReadiness(isWeak, advisorPromising, futureCandidate, risk),
            AdvisorStrength = ResolveStrength(advisorStrengthScore),
            Reasons = reasons,
            Metrics = new BeatGridWeakWindowLocalMetrics
            {
                LegacyIntervalCv = legacyCv,
                AdvisorIntervalCv = advisorCv,
                LegacyMedianIntervalSeconds = legacyMedian,
                AdvisorMedianIntervalSeconds = advisorMedian,
                LegacyBeatDensityPerSecond = legacyWindow.Length / duration,
                AdvisorBeatDensityPerSecond = advisorWindow.Length / duration,
                LocalCountRatio = localCountRatio,
                LocalBestOffsetMs = offsetMs,
                LocalZeroOffsetF1_70Ms = zeroF1,
                LocalBestOffsetF1_70Ms = bestF1,
                AgreementConfidenceScore = agreementScore,
                WeaknessScore = weaknessScore,
                AdvisorStrengthScore = advisorStrengthScore,
                CorrectionReadinessScore = readinessScore
            },
            ShouldApplyCorrection = false,
            Notes = ["Future correction candidate is diagnostic only."]
        };
    }

    private double CalculateWeaknessScore(double? legacyCv, double? medianInterval, double density, double bestF1)
    {
        var cvComponent = legacyCv.HasValue ? Clamp01((legacyCv.Value - _options.StrongLegacyIntervalCoefficientOfVariation) / 0.15) : 0.0;
        var densityComponent = density > 4.0 || medianInterval < 0.25 ? 1.0 : 0.0;
        var disagreementComponent = bestF1 < 0.40 ? 1.0 : bestF1 < 0.65 ? 0.6 : 0.0;
        return Math.Max(cvComponent, Math.Max(densityComponent, disagreementComponent * 0.7));
    }

    private double CalculateAdvisorStrengthScore(double? advisorCv, double bestF1, double? offsetMs, double? countRatio)
    {
        var cvComponent = advisorCv <= 0.10 ? 1.0 : advisorCv <= 0.18 ? 0.7 : advisorCv > 0.30 ? 0.0 : 0.4;
        var alignmentComponent = bestF1 >= 0.85 ? 1.0 : bestF1 >= 0.70 ? 0.75 : bestF1 >= 0.50 ? 0.4 : 0.0;
        var absOffset = Math.Abs(offsetMs ?? 999.0);
        var offsetComponent = absOffset <= 60.0 ? 1.0 : absOffset <= 120.0 ? 0.7 : 0.2;
        var ratioDelta = Math.Abs((countRatio ?? 999.0) - 1.0);
        var countComponent = ratioDelta <= _options.StrongCountRatioDelta ? 1.0 : ratioDelta <= _options.MaxCountRatioDelta ? 0.7 : 0.0;
        return (cvComponent * 0.25) + (alignmentComponent * 0.35) + (offsetComponent * 0.15) + (countComponent * 0.25);
    }

    private IEnumerable<BeatGridWeakWindowReason> ResolveReasons(
        double? legacyCv,
        double? advisorCv,
        double? medianInterval,
        double bestF1,
        double? countRatio,
        BeatGridPhaseAlignmentWindow? phaseWindow)
    {
        if (legacyCv > _options.MaxLegacyIntervalCoefficientOfVariation) yield return BeatGridWeakWindowReason.LegacyTempoInstability;
        if (medianInterval < 0.25) yield return BeatGridWeakWindowReason.LegacySparseOrDenseBeats;
        if (advisorCv < legacyCv) yield return BeatGridWeakWindowReason.AdvisorMoreStableIntervals;
        if (bestF1 >= _options.MinAdvisorAgreementF1_70Ms) yield return BeatGridWeakWindowReason.AdvisorStrongerLocalAgreement;
        if (bestF1 < 0.65) yield return BeatGridWeakWindowReason.CandidateDisagreement;
        if (countRatio.HasValue && Math.Abs(countRatio.Value - 1.0) > _options.MaxCountRatioDelta) yield return BeatGridWeakWindowReason.InsufficientEvidence;
        if (phaseWindow?.IsReliable == false) yield return BeatGridWeakWindowReason.PhaseAlignmentUnstable;
    }

    private BeatGridWeakWindowRiskLevel ResolveRisk(double bestF1, double? offsetMs, double? countRatio, BeatGridPhaseAlignmentWindow? phaseWindow)
    {
        var ratioDelta = Math.Abs((countRatio ?? 999.0) - 1.0);
        var absOffset = Math.Abs(offsetMs ?? 999.0);
        if (ratioDelta > 0.50 || phaseWindow is null) return BeatGridWeakWindowRiskLevel.Blocked;
        if (absOffset > 180.0 || bestF1 < 0.40 || ratioDelta > _options.MaxCountRatioDelta) return BeatGridWeakWindowRiskLevel.High;
        if (absOffset > 120.0 || bestF1 < 0.65) return BeatGridWeakWindowRiskLevel.Medium;
        return BeatGridWeakWindowRiskLevel.Low;
    }

    private static BeatGridWeakWindowCorrectionReadiness ResolveReadiness(bool weak, bool promising, bool futureCandidate, BeatGridWeakWindowRiskLevel risk)
    {
        if (risk == BeatGridWeakWindowRiskLevel.Blocked) return BeatGridWeakWindowCorrectionReadiness.Blocked;
        if (futureCandidate) return BeatGridWeakWindowCorrectionReadiness.CandidateForExperimentalCorrection;
        if (weak && promising) return BeatGridWeakWindowCorrectionReadiness.CandidateForReview;
        if (weak || promising) return BeatGridWeakWindowCorrectionReadiness.DiagnosticOnly;
        return BeatGridWeakWindowCorrectionReadiness.None;
    }

    private static BeatGridWeakWindowCandidateStrength ResolveStrength(double score)
    {
        if (score >= 0.90) return BeatGridWeakWindowCandidateStrength.VeryStrong;
        if (score >= 0.75) return BeatGridWeakWindowCandidateStrength.Strong;
        if (score >= 0.50) return BeatGridWeakWindowCandidateStrength.Moderate;
        return BeatGridWeakWindowCandidateStrength.Weak;
    }

    private static string ResolveGlobalReadiness(int futureCandidates, int weakCount, int blockedCount)
    {
        if (futureCandidates > 0) return "candidate-ready";
        if (weakCount > 0 && blockedCount == 0) return "diagnostic-ready";
        return "not-ready";
    }

    private static BeatGridPhaseAlignmentWindow? FindBestPhaseWindow(IReadOnlyList<BeatGridPhaseAlignmentWindow> windows, double start, double end)
    {
        return windows.OrderByDescending(window => CalculateOverlap(start, end, window.StartTimeSeconds, window.EndTimeSeconds)).FirstOrDefault();
    }

    private static BeatGridAgreementConfidenceWindow? FindBestAgreementWindow(IReadOnlyList<BeatGridAgreementConfidenceWindow> windows, double start, double end)
    {
        return windows.OrderByDescending(window => CalculateOverlap(start, end, window.StartTimeSeconds, window.EndTimeSeconds)).FirstOrDefault();
    }

    private static double CalculateOverlap(double leftStart, double leftEnd, double rightStart, double rightEnd)
    {
        return Math.Max(0.0, Math.Min(leftEnd, rightEnd) - Math.Max(leftStart, rightStart));
    }

    private static double? CalculateIntervalCv(double[] beats)
    {
        var intervals = beats.Zip(beats.Skip(1), (left, right) => right - left).Where(interval => interval > 0.0).ToArray();
        if (intervals.Length < 2) return null;
        var mean = intervals.Average();
        var variance = intervals.Select(interval => Math.Pow(interval - mean, 2.0)).Average();
        return Math.Sqrt(variance) / mean;
    }

    private static double? CalculateMedianInterval(double[] beats)
    {
        var intervals = beats.Zip(beats.Skip(1), (left, right) => right - left).Where(interval => interval > 0.0).Order().ToArray();
        if (intervals.Length == 0) return null;
        return intervals.Length % 2 == 1 ? intervals[intervals.Length / 2] : (intervals[(intervals.Length / 2) - 1] + intervals[intervals.Length / 2]) / 2.0;
    }

    private static double Clamp01(double value)
    {
        return Math.Min(1.0, Math.Max(0.0, value));
    }
}
