using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;

public sealed class BeatGridWeakWindowCorrectionAnalyzer
{
    private const double MinSpacingSeconds = 0.25;

    private readonly BeatGridWeakWindowCorrectionOptions _options;
    private readonly BeatGridCandidateFactory _candidateFactory;

    public BeatGridWeakWindowCorrectionAnalyzer(
        BeatGridWeakWindowCorrectionOptions? options = null,
        BeatGridCandidateFactory? candidateFactory = null)
    {
        _options = options ?? new BeatGridWeakWindowCorrectionOptions();
        _options.Validate();
        _candidateFactory = candidateFactory ?? new BeatGridCandidateFactory();
    }

    public BeatGridWeakWindowCorrectionResult Analyze(BeatGridCandidateSet? candidateSet)
    {
        if (_options.Mode == BeatGridWeakWindowCorrectionMode.Disabled)
        {
            return CreateNoCandidateResult("disabled", BeatGridWeakWindowCorrectionMode.Disabled, "correction-disabled");
        }

        var notAvailableReason = ResolveNotAvailableReason(candidateSet);
        if (notAvailableReason is not null)
        {
            return CreateNoCandidateResult("not-available", BeatGridWeakWindowCorrectionMode.DiagnosticsOnly, notAvailableReason);
        }

        var legacy = candidateSet!.Legacy!;
        var advisor = candidateSet.Advisor!;
        var allWeakWindows = candidateSet.WeakWindows!.Windows.ToArray();
        var blockers = allWeakWindows
            .Select(ResolveCorrectionBlocker)
            .ToArray();
        var blockerCounts = CountBlockers(blockers);
        var topBlockers = blockerCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Key)
            .Take(_options.MaxDiagnosticWindows)
            .ToArray();
        var diagnosticWindowCount = _options.MaxDiagnosticWindows == 0
            ? 0
            : allWeakWindows
                .Where(window => window.IsWeakWindow || window.AdvisorIsPromising || window.Metrics.CorrectionReadinessScore > 0.0)
                .OrderByDescending(window => window.Metrics.CorrectionReadinessScore)
                .ThenByDescending(window => window.Metrics.AdvisorStrengthScore)
                .Take(_options.MaxDiagnosticWindows)
                .Count();
        var candidateWindows = allWeakWindows
            .Where(IsCandidateWindow)
            .OrderByDescending(window => window.Metrics.CorrectionReadinessScore)
            .ThenBy(window => window.RiskLevel)
            .ThenBy(window => window.StartTimeSeconds)
            .Take(_options.MaxWindowsToCorrect)
            .ToArray();

        var windows = new List<BeatGridWeakWindowCorrectionWindow>();
        var correctedBeats = legacy.BeatTimes.ToList();

        foreach (var weakWindow in candidateWindows)
        {
            var evaluation = EvaluateWindow(weakWindow, legacy, advisor, correctedBeats);
            windows.Add(evaluation.Window);

            if (evaluation.Accepted && _options.Mode == BeatGridWeakWindowCorrectionMode.ExperimentalCandidate)
            {
                correctedBeats = evaluation.CorrectedBeats;
            }
        }

        var acceptedCount = windows.Count(window => window.Decision == BeatGridWeakWindowCorrectionDecision.CandidateCreated);
        var rejectedCount = windows.Count - acceptedCount;
        var plan = new BeatGridWeakWindowCorrectionPlan
        {
            Enabled = true,
            Mode = _options.Mode,
            WindowCount = candidateSet.WeakWindows!.Windows.Count,
            CandidateWindowCount = candidateWindows.Length,
            AcceptedWindowCount = acceptedCount,
            RejectedWindowCount = rejectedCount,
            BlockerCounts = blockerCounts,
            TopBlockers = topBlockers,
            Windows = windows,
            Notes = ["Correction candidate is experimental and diagnostic only."]
        };

        if (_options.Mode == BeatGridWeakWindowCorrectionMode.DiagnosticsOnly)
        {
            return new BeatGridWeakWindowCorrectionResult
            {
                CorrectedCandidate = null,
                Plan = plan,
                Diagnostics = CreateDiagnostics("diagnostics-only", _options.Mode, candidateSet.WeakWindows!, candidateWindows.Length, blockerCounts, topBlockers, diagnosticWindowCount, legacy, advisor, correctedBeats.ToArray(), acceptedCount, rejectedCount, correctedCandidate: null, rejectionReason: null)
            };
        }

        if (acceptedCount == 0)
        {
            return new BeatGridWeakWindowCorrectionResult
            {
                CorrectedCandidate = null,
                Plan = plan,
                Diagnostics = CreateDiagnostics("rejected", _options.Mode, candidateSet.WeakWindows!, candidateWindows.Length, blockerCounts, topBlockers, diagnosticWindowCount, legacy, advisor, correctedBeats.ToArray(), acceptedCount, rejectedCount, correctedCandidate: null, "no-correction-windows-accepted")
            };
        }

        var corrected = Dedupe(correctedBeats.Order().ToArray());
        var guardrailRejection = ValidateCorrectedCandidate(legacy, corrected);
        if (guardrailRejection is not null)
        {
            return new BeatGridWeakWindowCorrectionResult
            {
                CorrectedCandidate = null,
                Plan = plan,
                Diagnostics = CreateDiagnostics("rejected", _options.Mode, candidateSet.WeakWindows!, candidateWindows.Length, blockerCounts, topBlockers, diagnosticWindowCount, legacy, advisor, corrected, acceptedCount, rejectedCount, correctedCandidate: null, guardrailRejection)
            };
        }

        var correctedCandidate = _candidateFactory.FromBeatTimes(
            corrected,
            downbeatTimes: [],
            confidences: Enumerable.Repeat(0.75, corrected.Length).ToArray(),
            estimatedBpm: EstimateBpm(corrected),
            BeatGridCandidateSourceKind.WeakWindowCorrectedExperimental,
            BeatGridCandidateRole.CorrectedExperimental,
            "weak-window-corrected-experimental",
            "hybrid-experimental",
            "weak-window-correction-experimental",
            notes: ["Experimental corrected candidate; not selected as final."]);

        return new BeatGridWeakWindowCorrectionResult
        {
            CorrectedCandidate = correctedCandidate,
            Plan = plan,
            Diagnostics = CreateDiagnostics("candidate-created", _options.Mode, candidateSet.WeakWindows!, candidateWindows.Length, blockerCounts, topBlockers, diagnosticWindowCount, legacy, advisor, corrected, acceptedCount, rejectedCount, correctedCandidate, rejectionReason: null)
        };
    }

    private static string? ResolveNotAvailableReason(BeatGridCandidateSet? candidateSet)
    {
        if (candidateSet?.Legacy is null) return "legacy-candidate-missing";
        if (candidateSet.Advisor is null) return "advisor-not-available";
        if (candidateSet.WeakWindows is null) return "weak-windows-missing";
        if (candidateSet.PhaseAlignment is null) return "phase-alignment-missing";
        if (candidateSet.AgreementConfidence is null) return "agreement-confidence-missing";
        if (candidateSet.Advisor.Quality.IsDenseGrid || !candidateSet.Advisor.Quality.IsPlausible) return candidateSet.Advisor.Quality.RejectionReason ?? "advisor-rejected-or-dense";
        return null;
    }

    private bool IsCandidateWindow(BeatGridWeakWindow window)
    {
        return (window.FutureCorrectionCandidate || !_options.RequireFutureCorrectionCandidate)
            && window.CorrectionReadiness == BeatGridWeakWindowCorrectionReadiness.CandidateForExperimentalCorrection
            && window.RiskLevel is BeatGridWeakWindowRiskLevel.Low or BeatGridWeakWindowRiskLevel.Medium
            && !window.ShouldApplyCorrection
            && window.AdvisorIsPromising;
    }

    private (bool Accepted, BeatGridWeakWindowCorrectionWindow Window, List<double> CorrectedBeats) EvaluateWindow(
        BeatGridWeakWindow window,
        BeatGridCandidate legacy,
        BeatGridCandidate advisor,
        List<double> currentBeats)
    {
        var rejection = ResolveWindowRejection(window);
        if (rejection is not null)
        {
            return (false, CreateWindow(window, BeatGridWeakWindowCorrectionDecision.Rejected, rejection, currentBeats.Count, advisorBeatCount: 0), currentBeats);
        }

        var offsetSeconds = (window.Metrics.LocalBestOffsetMs ?? 0.0) / 1000.0;
        var advisorBeats = advisor.BeatTimes
            .Select(beat => beat + offsetSeconds)
            .Where(beat => beat >= window.StartTimeSeconds && beat <= window.EndTimeSeconds)
            .ToArray();
        var legacyBeatCountBefore = currentBeats.Count(beat => beat >= window.StartTimeSeconds && beat <= window.EndTimeSeconds);
        var nextBeats = currentBeats
            .Where(beat => beat < window.StartTimeSeconds || beat > window.EndTimeSeconds)
            .Concat(advisorBeats)
            .Order()
            .ToList();

        return (true, CreateWindow(window, BeatGridWeakWindowCorrectionDecision.CandidateCreated, null, legacyBeatCountBefore, advisorBeats.Length, nextBeats.Count), nextBeats);
    }

    private string? ResolveWindowRejection(BeatGridWeakWindow window)
    {
        if (window.RiskLevel is BeatGridWeakWindowRiskLevel.High or BeatGridWeakWindowRiskLevel.Blocked) return "high-risk-window";
        if (window.EndTimeSeconds - window.StartTimeSeconds > _options.MaxWindowDurationSeconds) return "window-duration-too-long";
        if (Math.Abs(window.Metrics.LocalBestOffsetMs ?? 999.0) > _options.MaxAllowedOffsetMs) return "offset-too-large";
        if (window.Metrics.AdvisorStrengthScore < _options.MinAdvisorStrengthScore) return "advisor-strength-too-low";
        if (window.Metrics.CorrectionReadinessScore < _options.MinCorrectionReadinessScore) return "correction-readiness-too-low";
        if (Math.Abs((window.Metrics.LocalCountRatio ?? 999.0) - 1.0) > _options.MaxAllowedCountRatioDelta) return "bad-count-ratio";
        return null;
    }

    private string ResolveCorrectionBlocker(BeatGridWeakWindow window)
    {
        if (!window.IsWeakWindow) return "not-weak-window";
        if (!window.AdvisorIsPromising) return "advisor-not-promising";
        if (!window.FutureCorrectionCandidate)
        {
            if (window.RiskLevel == BeatGridWeakWindowRiskLevel.Blocked) return "risk-blocked";
            if (window.RiskLevel == BeatGridWeakWindowRiskLevel.High) return "risk-high";
            if (window.Metrics.CorrectionReadinessScore < _options.MinCorrectionReadinessScore) return "correction-readiness-too-low";
            if (window.Metrics.AdvisorStrengthScore < _options.MinAdvisorStrengthScore) return "advisor-strength-too-low";
            if (Math.Abs((window.Metrics.LocalCountRatio ?? 999.0) - 1.0) > _options.MaxAllowedCountRatioDelta) return "bad-count-ratio";
            if (Math.Abs(window.Metrics.LocalBestOffsetMs ?? 999.0) > _options.MaxAllowedOffsetMs) return "offset-too-large";
            return "future-correction-candidate-false";
        }

        return ResolveWindowRejection(window) ?? "candidate-window";
    }

    private static IReadOnlyDictionary<string, int> CountBlockers(IEnumerable<string> blockers)
    {
        return blockers
            .GroupBy(blocker => blocker, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    private static BeatGridWeakWindowCorrectionWindow CreateWindow(
        BeatGridWeakWindow source,
        BeatGridWeakWindowCorrectionDecision decision,
        string? rejectionReason,
        int legacyBeatCount,
        int advisorBeatCount,
        int? correctedBeatCountAfter = null)
    {
        return new BeatGridWeakWindowCorrectionWindow
        {
            Index = source.Index,
            SourceWeakWindowIndex = source.Index,
            StartTimeSeconds = source.StartTimeSeconds,
            EndTimeSeconds = source.EndTimeSeconds,
            Decision = decision,
            Risk = MapRisk(source.RiskLevel),
            Readiness = source.CorrectionReadiness,
            LegacyBeatCountBefore = legacyBeatCount,
            AdvisorBeatCountUsed = advisorBeatCount,
            CorrectedBeatCountAfter = correctedBeatCountAfter ?? legacyBeatCount,
            ReplacementCountDelta = advisorBeatCount - legacyBeatCount,
            AppliedOffsetMs = source.Metrics.LocalBestOffsetMs,
            BoundaryAdjustmentApplied = false,
            RejectionReason = rejectionReason,
            Notes = ["Boundary preservation is conservative."]
        };
    }

    private static BeatGridWeakWindowCorrectionRisk MapRisk(BeatGridWeakWindowRiskLevel risk)
    {
        return risk switch
        {
            BeatGridWeakWindowRiskLevel.Low => BeatGridWeakWindowCorrectionRisk.Low,
            BeatGridWeakWindowRiskLevel.Medium => BeatGridWeakWindowCorrectionRisk.Medium,
            BeatGridWeakWindowRiskLevel.High => BeatGridWeakWindowCorrectionRisk.High,
            BeatGridWeakWindowRiskLevel.Blocked => BeatGridWeakWindowCorrectionRisk.Blocked,
            _ => BeatGridWeakWindowCorrectionRisk.Unknown
        };
    }

    private BeatGridWeakWindowCorrectionDiagnostics CreateDiagnostics(
        string status,
        BeatGridWeakWindowCorrectionMode mode,
        BeatGridWeakWindowDiagnostics weakWindows,
        int candidateWindowCount,
        IReadOnlyDictionary<string, int> blockerCounts,
        IReadOnlyList<string> topBlockers,
        int diagnosticWindowCount,
        BeatGridCandidate legacy,
        BeatGridCandidate advisor,
        double[] correctedBeats,
        int acceptedCount,
        int rejectedCount,
        BeatGridCandidate? correctedCandidate,
        string? rejectionReason)
    {
        return new BeatGridWeakWindowCorrectionDiagnostics
        {
            Enabled = true,
            CalibrationProfile = _options.CalibrationProfile,
            Status = status,
            Mode = mode,
            CorrectedCandidateCreated = correctedCandidate is not null,
            CorrectedCandidateId = correctedCandidate?.Id,
            AcceptedWindowCount = acceptedCount,
            RejectedWindowCount = rejectedCount,
            WeakWindowCount = weakWindows.WeakWindowCount,
            FutureCorrectionCandidateCount = weakWindows.FutureCorrectionCandidateCount,
            CandidateWindowCount = candidateWindowCount,
            BlockerCounts = blockerCounts,
            TopBlockers = topBlockers,
            DiagnosticWindowCount = diagnosticWindowCount,
            LegacyBeatCount = legacy.BeatTimes.Length,
            CorrectedBeatCount = correctedCandidate is null ? 0 : correctedBeats.Length,
            AdvisorBeatCount = advisor.BeatTimes.Length,
            CorrectedVsLegacyCountDelta = correctedCandidate is null ? 0 : correctedBeats.Length - legacy.BeatTimes.Length,
            CorrectedEstimatedBpm = correctedCandidate?.EstimatedBpm,
            CorrectedIsDenseGrid = correctedCandidate?.Quality.IsDenseGrid == true,
            ShouldModifyFinalGrid = false,
            ShouldSelectCorrectedCandidate = false,
            ShouldApplyCorrection = false,
            ExternalBenchmarkClaimStatus = "not-evaluated",
            RejectionReason = rejectionReason,
            Notes = ["Corrected candidate is experimental and is not selected as final."]
        };
    }

    private static BeatGridWeakWindowCorrectionResult CreateNoCandidateResult(string status, BeatGridWeakWindowCorrectionMode mode, string reason)
    {
        return new BeatGridWeakWindowCorrectionResult
        {
            CorrectedCandidate = null,
            Plan = new BeatGridWeakWindowCorrectionPlan
            {
                Enabled = true,
                Mode = mode,
                Notes = [reason]
            },
            Diagnostics = status == "not-available"
                ? BeatGridWeakWindowCorrectionDiagnostics.NotAvailable(reason)
                : new BeatGridWeakWindowCorrectionDiagnostics
                {
                    Enabled = true,
                    Status = status,
                    Mode = mode,
                    RejectionReason = reason,
                    ExternalBenchmarkClaimStatus = "not-evaluated",
                    Notes = [reason]
                }
        };
    }

    private string? ValidateCorrectedCandidate(BeatGridCandidate legacy, double[] corrected)
    {
        if (corrected.Length == 0) return "corrected-beat-count-zero";
        var median = MedianInterval(corrected);
        var duration = corrected[^1] - corrected[0];
        var density = duration > 0.0 ? corrected.Length / duration : double.PositiveInfinity;
        var bpm = EstimateBpm(corrected);
        if (density > _options.MaxCorrectedBeatDensityPerSecond) return "corrected-density-too-high";
        if (median < _options.MinCorrectedMedianIntervalSeconds) return "corrected-median-interval-too-low";
        if (bpm > 200.0) return "corrected-bpm-too-high";
        if (Math.Abs(corrected.Length / (double)legacy.BeatTimes.Length - 1.0) > _options.MaxAllowedCountRatioDelta) return "corrected-count-ratio-out-of-range";
        if (IntervalCv(corrected) > _options.MaxCorrectedIntervalCoefficientOfVariation) return "corrected-interval-cv-too-high";
        return null;
    }

    private static double[] Dedupe(double[] beats)
    {
        var result = new List<double>();
        foreach (var beat in beats)
        {
            if (result.Count == 0 || beat - result[^1] >= MinSpacingSeconds)
            {
                result.Add(beat);
            }
        }

        return result.ToArray();
    }

    private static double? EstimateBpm(double[] beats)
    {
        var median = MedianInterval(beats);
        return median > 0.0 ? 60.0 / median : null;
    }

    private static double? MedianInterval(double[] beats)
    {
        var intervals = beats.Zip(beats.Skip(1), (left, right) => right - left).Where(interval => interval > 0.0).Order().ToArray();
        if (intervals.Length == 0) return null;
        return intervals.Length % 2 == 1 ? intervals[intervals.Length / 2] : (intervals[(intervals.Length / 2) - 1] + intervals[intervals.Length / 2]) / 2.0;
    }

    private static double? IntervalCv(double[] beats)
    {
        var intervals = beats.Zip(beats.Skip(1), (left, right) => right - left).Where(interval => interval > 0.0).ToArray();
        if (intervals.Length < 2) return null;
        var mean = intervals.Average();
        var variance = intervals.Select(interval => Math.Pow(interval - mean, 2.0)).Average();
        return Math.Sqrt(variance) / mean;
    }
}
