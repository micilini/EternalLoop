using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;

public sealed class BeatGridPhaseAlignmentAnalyzer
{
    private readonly BeatGridPhaseAlignmentOptions _options;

    public BeatGridPhaseAlignmentAnalyzer(BeatGridPhaseAlignmentOptions? options = null)
    {
        _options = options ?? new BeatGridPhaseAlignmentOptions();
        _options.Validate();
    }

    public BeatGridPhaseAlignmentDiagnostics Analyze(
        BeatGridCandidate reference,
        BeatGridCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(candidate);

        if (reference.BeatTimes.Length == 0 || candidate.BeatTimes.Length == 0)
        {
            return BeatGridPhaseAlignmentDiagnostics.NotAvailable("missing-beats");
        }

        var countRatio = candidate.BeatTimes.Length / (double)reference.BeatTimes.Length;
        var zeroOffset = CalculateMetrics(reference.BeatTimes, candidate.BeatTimes, offsetMs: 0.0);
        var bestOffsetMs = FindBestOffset(reference.BeatTimes, candidate.BeatTimes);
        var bestOffset = CalculateMetrics(reference.BeatTimes, candidate.BeatTimes, bestOffsetMs);
        var windows = CalculateWindows(reference.BeatTimes, candidate.BeatTimes);
        var stableOffsets = windows
            .Where(window => window.IsReliable)
            .Select(window => window.BestOffsetMs)
            .ToArray();
        var offsetStabilityMad = stableOffsets.Length > 1
            ? CalculateMedianAbsoluteDeviation(stableOffsets)
            : (double?)null;
        var isOffsetStable = offsetStabilityMad <= _options.StableOffsetMaxMadMs;
        var confidence = ResolveConfidence(bestOffset.F1_70Ms, countRatio, bestOffsetMs, isOffsetStable, windows.Count);
        var unreliableReason = ResolveUnreliableReason(confidence, countRatio, bestOffsetMs, windows.Count);
        var status = ResolveStatus(confidence, bestOffsetMs, zeroOffset.F1_70Ms, bestOffset.F1_70Ms, unreliableReason);

        return new BeatGridPhaseAlignmentDiagnostics
        {
            Enabled = true,
            Status = status,
            ReferenceCandidateId = reference.Id,
            CandidateId = candidate.Id,
            ReferenceBeatCount = reference.BeatTimes.Length,
            CandidateBeatCount = candidate.BeatTimes.Length,
            CountRatio = countRatio,
            ZeroOffset = zeroOffset,
            BestOffsetMs = bestOffsetMs,
            BestOffset = bestOffset,
            ImprovementF1_70Ms = bestOffset.F1_70Ms - zeroOffset.F1_70Ms,
            OffsetDirection = ResolveOffsetDirection(bestOffsetMs),
            OffsetStabilityMadMs = offsetStabilityMad,
            IsOffsetStable = isOffsetStable,
            Confidence = confidence,
            ShouldApplyCorrection = false,
            Recommendation = "diagnostic-only-do-not-correct",
            Windows = windows,
            UnreliableReason = unreliableReason,
            Notes = ["Best offset is applied to the candidate for diagnostics only; beat times are not corrected."]
        };
    }

    private double FindBestOffset(double[] reference, double[] candidate)
    {
        var bestOffsetMs = 0.0;
        var bestF1 = -1.0;
        var bestMeanAbsError = double.PositiveInfinity;

        for (var offsetMs = _options.MinOffsetMs; offsetMs <= _options.MaxOffsetMs + 0.000001; offsetMs += _options.OffsetStepMs)
        {
            var metrics = CalculateSingleTolerance(reference, candidate, _options.PrimaryToleranceMs / 1000.0, offsetMs / 1000.0);
            var meanAbsError = metrics.MeanAbsError ?? double.PositiveInfinity;

            if (metrics.F1 > bestF1
                || (NearlyEqual(metrics.F1, bestF1) && meanAbsError < bestMeanAbsError)
                || (NearlyEqual(metrics.F1, bestF1) && NearlyEqual(meanAbsError, bestMeanAbsError) && Math.Abs(offsetMs) < Math.Abs(bestOffsetMs)))
            {
                bestF1 = metrics.F1;
                bestMeanAbsError = meanAbsError;
                bestOffsetMs = offsetMs;
            }
        }

        return bestOffsetMs;
    }

    private BeatGridPhaseAlignmentMetrics CalculateMetrics(double[] reference, double[] candidate, double offsetMs)
    {
        var metrics50 = CalculateSingleTolerance(reference, candidate, 0.050, offsetMs / 1000.0);
        var metrics70 = CalculateSingleTolerance(reference, candidate, 0.070, offsetMs / 1000.0);
        var metrics100 = CalculateSingleTolerance(reference, candidate, 0.100, offsetMs / 1000.0);

        return new BeatGridPhaseAlignmentMetrics
        {
            Precision50Ms = metrics50.Precision,
            Recall50Ms = metrics50.Recall,
            F1_50Ms = metrics50.F1,
            Precision70Ms = metrics70.Precision,
            Recall70Ms = metrics70.Recall,
            F1_70Ms = metrics70.F1,
            Precision100Ms = metrics100.Precision,
            Recall100Ms = metrics100.Recall,
            F1_100Ms = metrics100.F1,
            MatchedCount70Ms = metrics70.Matches,
            MeanAbsErrorMs = metrics70.MeanAbsError,
            MedianAbsErrorMs = metrics70.MedianAbsError,
            MeanSignedErrorMs = metrics70.MeanSignedError
        };
    }

    private IReadOnlyList<BeatGridPhaseAlignmentWindow> CalculateWindows(double[] reference, double[] candidate)
    {
        if (reference.Length < _options.MinBeatsForLocalWindow)
        {
            return [];
        }

        var windows = new List<BeatGridPhaseAlignmentWindow>();
        var windowIndex = 0;
        var windowSize = Math.Min(_options.LocalWindowBeatCount, reference.Length);

        for (var startIndex = 0; startIndex + _options.MinBeatsForLocalWindow <= reference.Length; startIndex += _options.LocalWindowHopBeatCount)
        {
            var endIndex = Math.Min(startIndex + windowSize, reference.Length);
            var referenceWindow = reference[startIndex..endIndex];

            if (referenceWindow.Length < _options.MinBeatsForLocalWindow)
            {
                continue;
            }

            var startTime = referenceWindow[0];
            var endTime = referenceWindow[^1];
            var candidateWindow = candidate
                .Where(beat => beat >= startTime - 0.5 && beat <= endTime + 0.5)
                .ToArray();

            if (candidateWindow.Length == 0)
            {
                windows.Add(new BeatGridPhaseAlignmentWindow
                {
                    Index = windowIndex++,
                    StartTimeSeconds = startTime,
                    EndTimeSeconds = endTime,
                    LegacyBeatCount = referenceWindow.Length,
                    AdvisorBeatCount = 0,
                    BestOffsetMs = 0.0,
                    ZeroOffsetF1_70Ms = 0.0,
                    BestOffsetF1_70Ms = 0.0,
                    ImprovementF1_70Ms = 0.0,
                    IsReliable = false,
                    Notes = ["candidate-window-empty"]
                });
                continue;
            }

            var zero = CalculateSingleTolerance(referenceWindow, candidateWindow, 0.070, offsetSeconds: 0.0);
            var bestOffsetMs = FindBestOffset(referenceWindow, candidateWindow);
            var best = CalculateSingleTolerance(referenceWindow, candidateWindow, 0.070, bestOffsetMs / 1000.0);

            windows.Add(new BeatGridPhaseAlignmentWindow
            {
                Index = windowIndex++,
                StartTimeSeconds = startTime,
                EndTimeSeconds = endTime,
                LegacyBeatCount = referenceWindow.Length,
                AdvisorBeatCount = candidateWindow.Length,
                BestOffsetMs = bestOffsetMs,
                ZeroOffsetF1_70Ms = zero.F1,
                BestOffsetF1_70Ms = best.F1,
                ImprovementF1_70Ms = best.F1 - zero.F1,
                IsReliable = best.F1 >= _options.LowConfidenceMinBestF1,
                Notes = ["Window offset is diagnostic only."]
            });
        }

        return windows;
    }

    private static ToleranceMetrics CalculateSingleTolerance(
        double[] reference,
        double[] candidate,
        double toleranceSeconds,
        double offsetSeconds)
    {
        if (reference.Length == 0 && candidate.Length == 0)
        {
            return new ToleranceMetrics(1.0, 1.0, 1.0, 0, null, null, null);
        }

        if (reference.Length == 0 || candidate.Length == 0)
        {
            return new ToleranceMetrics(0.0, 0.0, 0.0, 0, null, null, null);
        }

        var used = new bool[reference.Length];
        var signedErrorsMs = new List<double>();

        foreach (var candidateBeat in candidate)
        {
            var adjustedCandidate = candidateBeat + offsetSeconds;
            var bestIndex = -1;
            var bestDistance = double.PositiveInfinity;

            for (var index = 0; index < reference.Length; index++)
            {
                if (used[index])
                {
                    continue;
                }

                var distance = Math.Abs(reference[index] - adjustedCandidate);

                if (distance <= toleranceSeconds && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = index;
                }
            }

            if (bestIndex >= 0)
            {
                used[bestIndex] = true;
                signedErrorsMs.Add((adjustedCandidate - reference[bestIndex]) * 1000.0);
            }
        }

        var matches = signedErrorsMs.Count;
        var precision = matches / (double)candidate.Length;
        var recall = matches / (double)reference.Length;
        var f1 = precision + recall > 0.0
            ? 2.0 * precision * recall / (precision + recall)
            : 0.0;
        var absErrors = signedErrorsMs.Select(Math.Abs).Order().ToArray();

        return new ToleranceMetrics(
            precision,
            recall,
            f1,
            matches,
            absErrors.Length > 0 ? absErrors.Average() : null,
            CalculateMedian(absErrors),
            signedErrorsMs.Count > 0 ? signedErrorsMs.Average() : null);
    }

    private BeatGridPhaseAlignmentConfidence ResolveConfidence(
        double bestF1,
        double countRatio,
        double bestOffsetMs,
        bool isOffsetStable,
        int windowCount)
    {
        if (bestF1 < _options.LowConfidenceMinBestF1
            || countRatio < 1.0 - _options.MaxReliableCountRatioDelta
            || countRatio > 1.0 + _options.MaxReliableCountRatioDelta
            || Math.Abs(bestOffsetMs) > _options.MaxReliableAbsOffsetMs)
        {
            return BeatGridPhaseAlignmentConfidence.None;
        }

        if (bestF1 >= _options.HighConfidenceMinBestF1
            && countRatio >= 0.80
            && countRatio <= 1.20
            && (isOffsetStable || windowCount < 2))
        {
            return BeatGridPhaseAlignmentConfidence.High;
        }

        if (bestF1 >= _options.MediumConfidenceMinBestF1)
        {
            return BeatGridPhaseAlignmentConfidence.Medium;
        }

        return BeatGridPhaseAlignmentConfidence.Low;
    }

    private string? ResolveUnreliableReason(
        BeatGridPhaseAlignmentConfidence confidence,
        double countRatio,
        double bestOffsetMs,
        int windowCount)
    {
        if (countRatio < 1.0 - _options.MaxReliableCountRatioDelta || countRatio > 1.0 + _options.MaxReliableCountRatioDelta)
        {
            return "count-ratio-out-of-range";
        }

        if (Math.Abs(bestOffsetMs) > _options.MaxReliableAbsOffsetMs)
        {
            return "offset-out-of-range";
        }

        if (confidence == BeatGridPhaseAlignmentConfidence.None)
        {
            return "agreement-too-low";
        }

        if (windowCount == 0)
        {
            return "insufficient-local-windows";
        }

        return null;
    }

    private string ResolveStatus(
        BeatGridPhaseAlignmentConfidence confidence,
        double bestOffsetMs,
        double zeroF1,
        double bestF1,
        string? unreliableReason)
    {
        if (confidence == BeatGridPhaseAlignmentConfidence.None)
        {
            return "unreliable";
        }

        if (unreliableReason is "count-ratio-out-of-range" or "offset-out-of-range")
        {
            return "unreliable";
        }

        if (Math.Abs(bestOffsetMs) <= 10.0 && zeroF1 >= _options.HighConfidenceMinBestF1)
        {
            return "aligned";
        }

        return Math.Abs(bestOffsetMs) > 10.0 || bestF1 - zeroF1 > 0.000001
            ? "offset-detected"
            : "aligned";
    }

    private static string ResolveOffsetDirection(double bestOffsetMs)
    {
        if (bestOffsetMs > 0.000001)
        {
            return "candidate-needs-forward-shift";
        }

        if (bestOffsetMs < -0.000001)
        {
            return "candidate-needs-backward-shift";
        }

        return "none";
    }

    private static double? CalculateMedian(double[] values)
    {
        if (values.Length == 0)
        {
            return null;
        }

        return values.Length % 2 == 1
            ? values[values.Length / 2]
            : (values[(values.Length / 2) - 1] + values[values.Length / 2]) / 2.0;
    }

    private static double CalculateMedianAbsoluteDeviation(double[] values)
    {
        var sorted = values.Order().ToArray();
        var median = CalculateMedian(sorted) ?? 0.0;
        var deviations = sorted.Select(value => Math.Abs(value - median)).Order().ToArray();

        return CalculateMedian(deviations) ?? 0.0;
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) < 0.0000001;
    }

    private sealed record ToleranceMetrics(
        double Precision,
        double Recall,
        double F1,
        int Matches,
        double? MeanAbsError,
        double? MedianAbsError,
        double? MeanSignedError);
}
