using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;

public sealed class BeatGridAgreementConfidenceAnalyzer
{
    private readonly BeatGridAgreementConfidenceOptions _options;

    public BeatGridAgreementConfidenceAnalyzer(BeatGridAgreementConfidenceOptions? options = null)
    {
        _options = options ?? new BeatGridAgreementConfidenceOptions();
        _options.Validate();
    }

    public BeatGridAgreementConfidenceDiagnostics Analyze(BeatGridCandidateSet? candidateSet)
    {
        if (candidateSet is null)
        {
            return BeatGridAgreementConfidenceDiagnostics.NotAvailable("candidate-set-missing");
        }

        if (candidateSet.Legacy is null)
        {
            return BeatGridAgreementConfidenceDiagnostics.NotAvailable("legacy-candidate-missing");
        }

        if (candidateSet.Advisor is null)
        {
            return BeatGridAgreementConfidenceDiagnostics.NotAvailable("advisor-not-available");
        }

        if (candidateSet.PhaseAlignment is null)
        {
            return BeatGridAgreementConfidenceDiagnostics.NotAvailable("phase-alignment-missing");
        }

        if (string.Equals(candidateSet.PhaseAlignment.Status, "not-available", StringComparison.OrdinalIgnoreCase))
        {
            return BeatGridAgreementConfidenceDiagnostics.NotAvailable(candidateSet.PhaseAlignment.UnreliableReason ?? "phase-alignment-not-available");
        }

        if (candidateSet.Advisor.Quality.IsDenseGrid || !candidateSet.Advisor.Quality.IsPlausible)
        {
            return BeatGridAgreementConfidenceDiagnostics.NotAvailable(
                candidateSet.Advisor.Quality.RejectionReason ?? "advisor-quality-not-plausible");
        }

        var globalConfidence = CalculateGlobalConfidence(candidateSet.PhaseAlignment);
        var windows = candidateSet.PhaseAlignment.Windows
            .Select(CreateWindow)
            .ToArray();
        var highWindowCount = windows.Count(window => IsHighOrBetter(window.Confidence?.Level));
        var mediumOrBetterWindowCount = windows.Count(window => IsMediumOrBetter(window.Confidence?.Level));
        var highWindowRatio = windows.Length > 0
            ? highWindowCount / (double)windows.Length
            : 0.0;
        var readiness = ResolveReadiness(globalConfidence, highWindowRatio);

        return new BeatGridAgreementConfidenceDiagnostics
        {
            Enabled = true,
            Status = globalConfidence.IsReliable ? "evaluated" : "unreliable",
            LegacyCandidateId = candidateSet.Legacy.Id,
            AdvisorCandidateId = candidateSet.Advisor.Id,
            GlobalConfidence = globalConfidence,
            HighConfidenceWindowCount = highWindowCount,
            MediumOrBetterWindowCount = mediumOrBetterWindowCount,
            WindowCount = windows.Length,
            HighConfidenceWindowRatio = highWindowRatio,
            FutureFusionReadiness = readiness,
            FutureFusionReady = readiness == "candidate-ready",
            ShouldModifyFinalGrid = false,
            ShouldSelectAdvisor = false,
            ShouldApplyCorrection = false,
            ExternalBenchmarkClaimStatus = "not-evaluated",
            Windows = windows,
            UnreliableReason = globalConfidence.IsReliable ? null : globalConfidence.Reason,
            Notes = ["Agreement confidence is diagnostic only; final beat grid remains Legacy/BuiltIn."]
        };
    }

    private BeatGridAgreementConfidenceScore CalculateGlobalConfidence(BeatGridPhaseAlignmentDiagnostics phaseAlignment)
    {
        var f1 = Clamp01(phaseAlignment.BestOffset?.F1_70Ms ?? 0.0);
        var countRatio = phaseAlignment.CountRatio;
        var absOffsetMs = phaseAlignment.BestOffsetMs.HasValue
            ? Math.Abs(phaseAlignment.BestOffsetMs.Value)
            : (double?)null;
        var offsetStabilityMadMs = phaseAlignment.OffsetStabilityMadMs;
        var countPenalty = CalculateCountRatioPenalty(countRatio);
        var offsetPenalty = CalculateOffsetPenalty(absOffsetMs);
        var stabilityPenalty = CalculateStabilityPenalty(phaseAlignment.IsOffsetStable, offsetStabilityMadMs);
        var score = Clamp01((f1 * 0.55) + (countPenalty * 0.20) + (offsetPenalty * 0.15) + (stabilityPenalty * 0.10));
        var level = countPenalty <= 0.0 || offsetPenalty <= 0.0
            ? BeatGridAgreementConfidenceLevel.None
            : ResolveLevel(score, f1);
        var reliable = level != BeatGridAgreementConfidenceLevel.None
            && phaseAlignment.Confidence != BeatGridPhaseAlignmentConfidence.None;

        return new BeatGridAgreementConfidenceScore
        {
            Level = level,
            Score = score,
            F1_70Ms = f1,
            CountRatio = countRatio,
            AbsOffsetMs = absOffsetMs,
            OffsetStabilityMadMs = offsetStabilityMadMs,
            IsReliable = reliable,
            Reason = ResolveReason(level, f1, countPenalty, offsetPenalty),
            Notes = ["Score combines F1, count ratio, offset size, and offset stability."]
        };
    }

    private BeatGridAgreementConfidenceWindow CreateWindow(BeatGridPhaseAlignmentWindow window)
    {
        var confidence = CalculateWindowConfidence(window);
        var futureFusionCandidate = window.IsReliable && IsHighOrBetter(confidence.Level);

        return new BeatGridAgreementConfidenceWindow
        {
            Index = window.Index,
            StartTimeSeconds = window.StartTimeSeconds,
            EndTimeSeconds = window.EndTimeSeconds,
            LegacyBeatCount = window.LegacyBeatCount,
            AdvisorBeatCount = window.AdvisorBeatCount,
            ZeroOffsetF1_70Ms = window.ZeroOffsetF1_70Ms,
            BestOffsetF1_70Ms = window.BestOffsetF1_70Ms,
            BestOffsetMs = window.BestOffsetMs,
            Confidence = confidence,
            FutureFusionCandidate = futureFusionCandidate,
            UnreliableReason = confidence.IsReliable ? null : confidence.Reason,
            Notes = ["Window readiness is diagnostic only."]
        };
    }

    private BeatGridAgreementConfidenceScore CalculateWindowConfidence(BeatGridPhaseAlignmentWindow window)
    {
        var f1 = Clamp01(window.BestOffsetF1_70Ms);
        var absOffsetMs = Math.Abs(window.BestOffsetMs);
        var offsetPenalty = CalculateOffsetPenalty(absOffsetMs);
        var reliabilityPenalty = window.IsReliable ? 1.0 : 0.0;
        var score = Clamp01((f1 * 0.75) + (offsetPenalty * 0.15) + (reliabilityPenalty * 0.10));
        var level = ResolveWindowLevel(f1, absOffsetMs, window.IsReliable);

        return new BeatGridAgreementConfidenceScore
        {
            Level = level,
            Score = score,
            F1_70Ms = f1,
            AbsOffsetMs = absOffsetMs,
            IsReliable = window.IsReliable && level != BeatGridAgreementConfidenceLevel.None,
            Reason = ResolveReason(level, f1, countPenalty: 1.0, offsetPenalty),
            Notes = ["Window confidence uses local phase alignment diagnostics."]
        };
    }

    private string ResolveReadiness(BeatGridAgreementConfidenceScore globalConfidence, double highWindowRatio)
    {
        if (globalConfidence.Reason is "count-ratio-out-of-range" or "offset-too-large")
        {
            return "not-ready";
        }

        if (globalConfidence.Level is BeatGridAgreementConfidenceLevel.None or BeatGridAgreementConfidenceLevel.Low
            && highWindowRatio < 0.25)
        {
            return "not-ready";
        }

        if (IsHighOrBetter(globalConfidence.Level)
            && highWindowRatio >= _options.FutureFusionReadinessMinHighWindowRatio
            && globalConfidence.F1_70Ms >= _options.FutureFusionReadinessMinGlobalF1)
        {
            return "candidate-ready";
        }

        if (globalConfidence.Level >= BeatGridAgreementConfidenceLevel.Medium || highWindowRatio >= 0.25)
        {
            return "diagnostic-ready";
        }

        return "not-ready";
    }

    private BeatGridAgreementConfidenceLevel ResolveLevel(double score, double f1)
    {
        if (score >= 0.90 && f1 >= _options.VeryHighAgreementF1)
        {
            return BeatGridAgreementConfidenceLevel.VeryHigh;
        }

        if (score >= 0.80 && f1 >= _options.HighAgreementF1)
        {
            return BeatGridAgreementConfidenceLevel.High;
        }

        if (score >= 0.60 && f1 >= _options.MediumAgreementF1)
        {
            return BeatGridAgreementConfidenceLevel.Medium;
        }

        if (score >= 0.35 && f1 >= _options.LowAgreementF1)
        {
            return BeatGridAgreementConfidenceLevel.Low;
        }

        return BeatGridAgreementConfidenceLevel.None;
    }

    private BeatGridAgreementConfidenceLevel ResolveWindowLevel(double f1, double absOffsetMs, bool reliable)
    {
        if (f1 >= _options.VeryHighAgreementF1 && absOffsetMs <= _options.MaxHighConfidenceAbsOffsetMs && reliable)
        {
            return BeatGridAgreementConfidenceLevel.VeryHigh;
        }

        if (f1 >= _options.HighAgreementF1 && absOffsetMs <= 90.0 && reliable)
        {
            return BeatGridAgreementConfidenceLevel.High;
        }

        if (f1 >= _options.MediumAgreementF1 && reliable)
        {
            return BeatGridAgreementConfidenceLevel.Medium;
        }

        if (f1 >= _options.LowAgreementF1)
        {
            return BeatGridAgreementConfidenceLevel.Low;
        }

        return BeatGridAgreementConfidenceLevel.None;
    }

    private static bool IsHighOrBetter(BeatGridAgreementConfidenceLevel? level)
    {
        return level is BeatGridAgreementConfidenceLevel.High or BeatGridAgreementConfidenceLevel.VeryHigh;
    }

    private static bool IsMediumOrBetter(BeatGridAgreementConfidenceLevel? level)
    {
        return level is BeatGridAgreementConfidenceLevel.Medium or BeatGridAgreementConfidenceLevel.High or BeatGridAgreementConfidenceLevel.VeryHigh;
    }

    private double CalculateCountRatioPenalty(double? countRatio)
    {
        if (!countRatio.HasValue)
        {
            return 0.0;
        }

        var delta = Math.Abs(countRatio.Value - 1.0);

        if (delta <= _options.MaxCountRatioDeltaHigh)
        {
            return 1.0;
        }

        if (delta <= _options.MaxCountRatioDeltaMedium)
        {
            return 0.75;
        }

        if (delta <= 0.50)
        {
            return 0.40;
        }

        return 0.0;
    }

    private double CalculateOffsetPenalty(double? absOffsetMs)
    {
        if (!absOffsetMs.HasValue)
        {
            return 0.0;
        }

        if (absOffsetMs.Value <= _options.MaxHighConfidenceAbsOffsetMs)
        {
            return 1.0;
        }

        if (absOffsetMs.Value <= _options.MaxMediumConfidenceAbsOffsetMs)
        {
            return 0.75;
        }

        if (absOffsetMs.Value <= 180.0)
        {
            return 0.40;
        }

        return 0.0;
    }

    private double CalculateStabilityPenalty(bool isOffsetStable, double? offsetStabilityMadMs)
    {
        if (isOffsetStable)
        {
            return 1.0;
        }

        if (offsetStabilityMadMs <= 50.0)
        {
            return 0.75;
        }

        return 0.45;
    }

    private static string ResolveReason(
        BeatGridAgreementConfidenceLevel level,
        double f1,
        double countPenalty,
        double offsetPenalty)
    {
        if (level == BeatGridAgreementConfidenceLevel.None && f1 < 0.40)
        {
            return "agreement-f1-too-low";
        }

        if (countPenalty <= 0.0)
        {
            return "count-ratio-out-of-range";
        }

        if (offsetPenalty <= 0.0)
        {
            return "offset-too-large";
        }

        return level.ToString().ToLowerInvariant();
    }

    private static double Clamp01(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }

        return value > 1.0 ? 1.0 : value;
    }
}
