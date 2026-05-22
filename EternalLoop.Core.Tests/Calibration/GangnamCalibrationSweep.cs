using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.Tests.Calibration;

public sealed class GangnamCalibrationSweep
{
    private const double SourceLowerRatio = 0.10;
    private const double TooDenseRatio = 1.80;
    private const int MaxSweepCandidates = 120;

    private readonly GangnamCalibrationRunner _runner;

    public GangnamCalibrationSweep(GangnamCalibrationRunner runner)
    {
        _runner = runner;
    }

    public IReadOnlyList<GangnamCalibrationCandidate> Run(
        TrackAnalysis analysis,
        string presetId,
        InfiniteJukeboxSvgSummary svg,
        EternalLoopBranchSummary? balancedBaseline,
        string outputDirectory)
    {
        var candidates = BuildCandidates(presetId)
            .Select((options, index) =>
            {
                var summary = _runner.RunPreset(
                    analysis,
                    $"{presetId}-candidate-{index}",
                    outputDirectory,
                    options,
                    writeDiagnostics: false);

                return new GangnamCalibrationCandidate(
                    presetId,
                    options,
                    summary,
                    ScoreCandidate(summary, svg, balancedBaseline));
            })
            .OrderBy(candidate => candidate.CandidateScore)
            .ThenByDescending(candidate => candidate.Summary.EdgeCount)
            .Take(10)
            .ToArray();

        return candidates;
    }

    private static IEnumerable<BranchFindingOptions> BuildCandidates(string presetId)
    {
        var current = GangnamCalibrationRunner.CreatePresetOptions(presetId);
        var isWild = string.Equals(presetId, TuningPresetCatalog.WildId, StringComparison.Ordinal);
        var thresholds = isWild
            ? new[] { current.SimilarityThreshold, current.SimilarityThreshold - 0.02, current.SimilarityThreshold - 0.04 }
            : [current.SimilarityThreshold, current.SimilarityThreshold - 0.02, current.SimilarityThreshold - 0.04, current.SimilarityThreshold - 0.06];
        var lookaheads = isWild
            ? new[] { current.LookaheadDepth, Math.Max(1, current.LookaheadDepth - 1) }
            : [current.LookaheadDepth, Math.Max(2, current.LookaheadDepth - 1)];
        var continuations = isWild
            ? new[] { current.ContinuationLookaheadDepth, Math.Max(2, current.ContinuationLookaheadDepth - 2) }
            : [current.ContinuationLookaheadDepth, Math.Max(3, current.ContinuationLookaheadDepth - 2)];
        var margins = new[] { current.ContinuationThresholdMargin, 0.0 };
        var microProfiles = new[]
        {
            new MicroProfile(
                current.MicrosegmentPenaltyStartThreshold,
                current.MicrosegmentRejectionThreshold,
                current.MicrosegmentPenaltyStrength),
            new MicroProfile(
                current.MicrosegmentPenaltyStartThreshold - 0.05,
                current.MicrosegmentRejectionThreshold - 0.08,
                current.MicrosegmentPenaltyStrength * 0.75),
            new MicroProfile(
                current.MicrosegmentPenaltyStartThreshold - 0.10,
                current.MicrosegmentRejectionThreshold - 0.14,
                current.MicrosegmentPenaltyStrength * 0.50)
        };
        var sourceRatios = isWild
            ? new[] { current.MaxBranchSourceRatio, Math.Min(0.50, current.MaxBranchSourceRatio + 0.08), Math.Min(0.60, current.MaxBranchSourceRatio + 0.16) }
            : [current.MaxBranchSourceRatio, Math.Min(0.34, current.MaxBranchSourceRatio + 0.06), Math.Min(0.42, current.MaxBranchSourceRatio + 0.12)];

        var candidateCount = 0;
        foreach (var threshold in thresholds)
        foreach (var lookahead in lookaheads)
        foreach (var continuation in continuations)
        foreach (var margin in margins)
        foreach (var microProfile in microProfiles)
        foreach (var sourceRatio in sourceRatios)
        {
            yield return Copy(current, threshold, lookahead, continuation, margin, microProfile.Start, microProfile.Rejection, microProfile.Strength, sourceRatio);
            candidateCount++;
            if (candidateCount >= MaxSweepCandidates)
            {
                yield break;
            }
        }
    }

    private static double ScoreCandidate(
        EternalLoopBranchSummary summary,
        InfiniteJukeboxSvgSummary svg,
        EternalLoopBranchSummary? balancedBaseline)
    {
        var edgeDistance = svg.BranchPathCount <= 0
            ? 1.0
            : Math.Abs(summary.EdgeCount - svg.BranchPathCount) / (double)svg.BranchPathCount;
        var sourcePenalty = summary.SourceRatio < SourceLowerRatio ? 1.0 : 0.0;
        var emptyPenalty = summary.EdgeCount == 0 ? 10.0 : 0.0;
        var tooDensePenalty = summary.EdgeCount > svg.BranchPathCount * TooDenseRatio ? 1.0 : 0.0;
        var wildPenalty = balancedBaseline is not null && summary.EdgeCount < balancedBaseline.EdgeCount ? 5.0 : 0.0;

        return edgeDistance + sourcePenalty + emptyPenalty + tooDensePenalty + wildPenalty;
    }

    private static BranchFindingOptions Copy(
        BranchFindingOptions current,
        double threshold,
        int lookahead,
        int continuation,
        double margin,
        double microStart,
        double microReject,
        double microStrength,
        double sourceRatio)
    {
        return new BranchFindingOptions
        {
            SimilarityThreshold = Math.Clamp(threshold, 0.0, 1.0),
            LookaheadDepth = lookahead,
            MinJumpDistance = current.MinJumpDistance,
            MaxBranchesPerBeat = current.MaxBranchesPerBeat,
            LandingOffsetBeats = current.LandingOffsetBeats,
            ContinuationLookaheadDepth = continuation,
            ContinuationThresholdMargin = Math.Clamp(margin, 0.0, 1.0),
            TimbreWeight = current.TimbreWeight,
            PitchWeight = current.PitchWeight,
            LoudnessWeight = current.LoudnessWeight,
            BarPositionWeight = current.BarPositionWeight,
            UseAiSimilarity = false,
            AiRejectionThreshold = current.AiRejectionThreshold,
            AiPenaltyStartThreshold = current.AiPenaltyStartThreshold,
            AiPenaltyStrength = current.AiPenaltyStrength,
            UseDurationSimilarityGate = current.UseDurationSimilarityGate,
            DurationPenaltyStartRatio = current.DurationPenaltyStartRatio,
            DurationRejectionRatio = current.DurationRejectionRatio,
            DurationPenaltyStrength = current.DurationPenaltyStrength,
            UseConfidencePenalty = current.UseConfidencePenalty,
            ConfidencePenaltyStart = current.ConfidencePenaltyStart,
            ConfidenceRejectionThreshold = current.ConfidenceRejectionThreshold,
            ConfidencePenaltyStrength = current.ConfidencePenaltyStrength,
            MetricPositionMode = current.MetricPositionMode,
            MetricPositionPenaltyStrength = current.MetricPositionPenaltyStrength,
            MetricPositionRejectionThreshold = current.MetricPositionRejectionThreshold,
            TargetBranchSourceRatio = current.TargetBranchSourceRatio,
            MaxBranchSourceRatio = Math.Clamp(sourceRatio, 0.0, 1.0),
            UseMicrosegmentSimilarity = current.UseMicrosegmentSimilarity,
            MicrosegmentCount = current.MicrosegmentCount,
            MicrosegmentPenaltyStartThreshold = Math.Clamp(microStart, 0.0, 1.0),
            MicrosegmentRejectionThreshold = Math.Clamp(microReject, 0.0, Math.Clamp(microStart, 0.0, 1.0)),
            MicrosegmentPenaltyStrength = Math.Clamp(microStrength, 0.0, 1.0)
        };
    }
}

public sealed record GangnamCalibrationCandidate(
    string Preset,
    BranchFindingOptions Options,
    EternalLoopBranchSummary Summary,
    double CandidateScore);

file sealed record MicroProfile(double Start, double Rejection, double Strength);
