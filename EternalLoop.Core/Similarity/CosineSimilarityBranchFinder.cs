using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.Similarity;

public sealed class CosineSimilarityBranchFinder : IBranchFinder
{
    private const int MinimumAdaptiveTargetEdges = 180;
    private const int MaximumAdaptiveTargetEdges = 650;
    private const int MaximumContinuationLookaheadDepth = 12;
    private const double AnchorScoreWeight = 0.25;
    private const double PhraseScoreWeight = 0.75;

    public IReadOnlyList<JukeboxEdge> FindBranches(
        IReadOnlyList<Beat> beats,
        BranchFindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(options);

        var matrix = SelfSimilarityMatrix.Compute(beats, options);

        return FindBranchesCore(beats, options, matrix, isAiActiveForThisAnalysis: false);
    }

    public IReadOnlyList<JukeboxEdge> FindBranches(
        TrackAnalysis analysis,
        BranchFindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(options);

        var matrix = SelfSimilarityMatrix.Compute(analysis, options);
        var isAiActive = options.UseAiSimilarity && analysis.Ai is { BeatEmbeddings.Count: > 0 };

        return FindBranchesCore(analysis.Beats, options, matrix, isAiActive);
    }

    private static IReadOnlyList<JukeboxEdge> FindBranchesCore(
        IReadOnlyList<Beat> beats,
        BranchFindingOptions options,
        double[,] matrix,
        bool isAiActiveForThisAnalysis)
    {
        var lookahead = Math.Max(0, options.LookaheadDepth);
        if (beats.Count == 0 || beats.Count <= lookahead + 1)
        {
            return [];
        }

        var minJumpDistance = Math.Max(1, options.MinJumpDistance);
        var maxBranchesPerBeat = Math.Max(1, options.MaxBranchesPerBeat);
        var landingOffset = Math.Clamp(options.LandingOffsetBeats, 0, Math.Max(1, lookahead + 1));
        var threshold = Math.Clamp(options.SimilarityThreshold, 0.0, 1.0);
        threshold = ApplyAiThresholdFloor(threshold, options, isAiActiveForThisAnalysis);
        var continuationLookahead = Math.Clamp(
            Math.Max(options.ContinuationLookaheadDepth, lookahead),
            lookahead,
            MaximumContinuationLookaheadDepth);
        var continuationThreshold = Math.Clamp(
            threshold + Math.Max(0.0, options.ContinuationThresholdMargin),
            0.0,
            1.0);
        var targetMaxEdges = GetAdaptiveTargetMaxEdges(beats.Count, maxBranchesPerBeat, options);

        var edges = BuildEdges(
            matrix,
            threshold,
            lookahead,
            minJumpDistance,
            maxBranchesPerBeat,
            landingOffset,
            continuationLookahead,
            new LookaheadValidationOptions(
                threshold,
                options.AnchorLookaheadPassRatio,
                options.AnchorLookaheadDropTolerance),
            new LookaheadValidationOptions(
                continuationThreshold,
                options.ContinuationLookaheadPassRatio,
                options.ContinuationLookaheadDropTolerance));
        if ((edges.Count == 0 && HasCandidates(beats.Count, lookahead, minJumpDistance)) ||
            edges.Count > targetMaxEdges)
        {
            threshold = AdaptiveThresholdSelector.Select(
                matrix,
                minJumpDistance,
                lookahead,
                targetMaxEdges: targetMaxEdges,
                fallbackThreshold: threshold);
            threshold = ApplyAiThresholdFloor(threshold, options, isAiActiveForThisAnalysis);

            continuationThreshold = Math.Clamp(
                threshold + Math.Max(0.0, options.ContinuationThresholdMargin),
                0.0,
                1.0);
            edges = BuildEdges(
                matrix,
                threshold,
                lookahead,
                minJumpDistance,
                maxBranchesPerBeat,
                landingOffset,
                continuationLookahead,
                new LookaheadValidationOptions(
                    threshold,
                    options.AnchorLookaheadPassRatio,
                    options.AnchorLookaheadDropTolerance),
                new LookaheadValidationOptions(
                    continuationThreshold,
                    options.ContinuationLookaheadPassRatio,
                    options.ContinuationLookaheadDropTolerance));
        }

        var densityLimitedEdges = BranchSourceDensityLimiter.Limit(
            edges,
            beats.Count,
            maxBranchesPerBeat,
            options.TargetBranchSourceRatio,
            options.MaxBranchSourceRatio);

        var orderedEdges = densityLimitedEdges
            .OrderBy(edge => edge.FromBeat)
            .ThenByDescending(edge => edge.Similarity)
            .ThenBy(edge => edge.ToBeat)
            .ToArray();

        if (orderedEdges.Length <= MaximumAdaptiveTargetEdges)
        {
            return orderedEdges;
        }

        return orderedEdges
            .OrderByDescending(edge => edge.Similarity)
            .ThenBy(edge => edge.FromBeat)
            .ThenBy(edge => edge.ToBeat)
            .Take(MaximumAdaptiveTargetEdges)
            .OrderBy(edge => edge.FromBeat)
            .ThenByDescending(edge => edge.Similarity)
            .ThenBy(edge => edge.ToBeat)
            .ToArray();
    }

    private static double ApplyAiThresholdFloor(
        double threshold,
        BranchFindingOptions options,
        bool isAiActiveForThisAnalysis)
    {
        if (!isAiActiveForThisAnalysis)
        {
            return threshold;
        }

        return Math.Max(threshold, Math.Clamp(options.AiRejectionThreshold, 0.0, 1.0));
    }

    private static List<JukeboxEdge> BuildEdges(
        double[,] matrix,
        double threshold,
        int lookahead,
        int minJumpDistance,
        int maxBranchesPerBeat,
        int landingOffset,
        int continuationLookahead,
        LookaheadValidationOptions anchorValidation,
        LookaheadValidationOptions continuationValidation)
    {
        var n = matrix.GetLength(0);
        var edges = new List<JukeboxEdge>();

        for (var i = 0; i < n; i++)
        {
            if (i + lookahead >= n)
            {
                continue;
            }

            var candidates = new List<JukeboxEdge>();

            for (var j = 0; j < n; j++)
            {
                var anchorBeat = j;
                var landingBeat = anchorBeat + landingOffset;

                if (i == anchorBeat ||
                    landingBeat < 0 ||
                    landingBeat >= n ||
                    i == landingBeat ||
                    Math.Abs(i - landingBeat) < minJumpDistance ||
                    anchorBeat + lookahead >= n)
                {
                    continue;
                }

                var sourceContinuationStart = i + landingOffset;

                if (sourceContinuationStart < 0 ||
                    sourceContinuationStart >= n ||
                    sourceContinuationStart + continuationLookahead >= n ||
                    landingBeat + continuationLookahead >= n)
                {
                    continue;
                }

                if (!TryGetLookaheadScore(matrix, i, anchorBeat, lookahead, anchorValidation, out var anchorScore))
                {
                    continue;
                }

                if (!TryGetLookaheadScore(
                        matrix,
                        sourceContinuationStart,
                        landingBeat,
                        continuationLookahead,
                        continuationValidation,
                        out var continuationScore))
                {
                    continue;
                }

                var combinedScore = (anchorScore * AnchorScoreWeight) + (continuationScore * PhraseScoreWeight);

                candidates.Add(new JukeboxEdge
                {
                    FromBeat = i,
                    ToBeat = landingBeat,
                    Similarity = Math.Clamp(combinedScore, 0.0, 1.0)
                });
            }

            edges.AddRange(candidates
                .GroupBy(edge => edge.ToBeat)
                .Select(group => group
                    .OrderByDescending(edge => edge.Similarity)
                    .First())
                .OrderByDescending(edge => edge.Similarity)
                .ThenByDescending(edge => Math.Abs(edge.ToBeat - edge.FromBeat))
                .ThenBy(edge => edge.ToBeat)
                .Take(maxBranchesPerBeat));
        }

        return edges;
    }

    private static bool TryGetLookaheadScore(
        double[,] matrix,
        int fromBeat,
        int toBeat,
        int lookahead,
        LookaheadValidationOptions validation,
        out double score)
    {
        var count = lookahead + 1;
        var threshold = Math.Clamp(validation.Threshold, 0.0, 1.0);
        var passRatio = Math.Clamp(validation.PassRatio, 0.0, 1.0);
        var dropTolerance = Math.Clamp(validation.DropTolerance, 0.0, 1.0);
        var floor = Math.Clamp(threshold - dropTolerance, 0.0, 1.0);
        var requiredAverage = Math.Clamp(threshold - (dropTolerance * 0.50), 0.0, 1.0);
        double sum = 0.0;
        double minimum = 1.0;
        var passed = 0;

        for (var k = 0; k <= lookahead; k++)
        {
            var value = matrix[fromBeat + k, toBeat + k];

            if (!double.IsFinite(value))
            {
                score = 0.0;
                return false;
            }

            sum += value;
            minimum = Math.Min(minimum, value);
            if (value >= threshold)
            {
                passed++;
            }
        }

        var average = sum / count;
        var actualPassRatio = passed / (double)count;

        if (minimum < floor ||
            average < requiredAverage ||
            actualPassRatio < passRatio)
        {
            score = 0.0;
            return false;
        }

        score = Math.Clamp((average * 0.85) + (minimum * 0.15), 0.0, 1.0);
        return true;
    }

    private static bool HasCandidates(int beatCount, int lookahead, int minJumpDistance)
    {
        for (var i = 0; i < beatCount; i++)
        {
            if (i + lookahead >= beatCount)
            {
                continue;
            }

            for (var j = 0; j < beatCount; j++)
            {
                if (i != j && Math.Abs(i - j) >= minJumpDistance && j + lookahead < beatCount)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int GetAdaptiveTargetMaxEdges(
        int beatCount,
        int maxBranchesPerBeat,
        BranchFindingOptions options)
    {
        var hardSourceLimit = CalculateHardSourceLimit(beatCount, options.MaxBranchSourceRatio);
        var presetBudget = hardSourceLimit * Math.Max(1, maxBranchesPerBeat);
        var minimum = beatCount < MinimumAdaptiveTargetEdges
            ? Math.Max(1, beatCount)
            : 1;

        return Math.Clamp(
            presetBudget,
            minimum,
            MaximumAdaptiveTargetEdges);
    }

    private static int CalculateHardSourceLimit(int beatCount, double maxBranchSourceRatio)
    {
        if (beatCount <= 0)
        {
            return 0;
        }

        if (!double.IsFinite(maxBranchSourceRatio) || maxBranchSourceRatio <= 0.000001)
        {
            return beatCount;
        }

        var ratio = Math.Clamp(maxBranchSourceRatio, 0.0, 1.0);
        var sourceLimit = (int)Math.Ceiling(beatCount * ratio);
        return Math.Clamp(sourceLimit, 1, beatCount);
    }

    private readonly record struct LookaheadValidationOptions(
        double Threshold,
        double PassRatio,
        double DropTolerance);
}
