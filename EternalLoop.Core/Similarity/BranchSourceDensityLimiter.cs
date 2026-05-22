using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.Similarity;

public static class BranchSourceDensityLimiter
{
    private const double MinimumPositiveRatio = 0.000001;

    public static IReadOnlyList<JukeboxEdge> Limit(
        IReadOnlyList<JukeboxEdge> edges,
        int beatCount,
        int maxBranchesPerBeat,
        double targetBranchSourceRatio,
        double maxBranchSourceRatio)
    {
        ArgumentNullException.ThrowIfNull(edges);

        if (edges.Count == 0 || beatCount <= 0)
        {
            return [];
        }

        var sourceLimit = CalculateSourceLimit(
            beatCount,
            targetBranchSourceRatio,
            maxBranchSourceRatio);

        if (sourceLimit <= 0)
        {
            return [];
        }

        var safeMaxBranchesPerBeat = Math.Max(1, maxBranchesPerBeat);
        var sourceGroups = edges
            .GroupBy(edge => edge.FromBeat)
            .Select(group => new BranchSourceGroup(
                group.Key,
                group
                    .OrderByDescending(edge => edge.Similarity)
                    .ThenBy(edge => edge.ToBeat)
                    .Take(safeMaxBranchesPerBeat)
                    .ToArray()))
            .Where(group => group.Edges.Count > 0)
            .ToArray();

        if (sourceGroups.Length <= sourceLimit)
        {
            return sourceGroups
                .SelectMany(group => group.Edges)
                .OrderBy(edge => edge.FromBeat)
                .ThenByDescending(edge => edge.Similarity)
                .ThenBy(edge => edge.ToBeat)
                .ToArray();
        }

        return sourceGroups
            .OrderByDescending(group => group.BestScore)
            .ThenByDescending(group => group.EdgeCount)
            .ThenBy(group => group.FromBeat)
            .Take(sourceLimit)
            .SelectMany(group => group.Edges)
            .OrderBy(edge => edge.FromBeat)
            .ThenByDescending(edge => edge.Similarity)
            .ThenBy(edge => edge.ToBeat)
            .ToArray();
    }

    private static int CalculateSourceLimit(
        int beatCount,
        double targetBranchSourceRatio,
        double maxBranchSourceRatio)
    {
        var maxRatio = ClampRatio(maxBranchSourceRatio);
        var targetRatio = ClampRatio(targetBranchSourceRatio);

        if (maxRatio <= 0.0 && targetRatio <= 0.0)
        {
            return 0;
        }

        var effectiveRatio = targetRatio > 0.0
            ? targetRatio
            : maxRatio;

        if (maxRatio > 0.0)
        {
            effectiveRatio = Math.Min(effectiveRatio, maxRatio);
        }

        var sourceLimit = (int)Math.Ceiling(beatCount * effectiveRatio);
        return Math.Clamp(sourceLimit, 1, beatCount);
    }

    private static double ClampRatio(double ratio)
    {
        if (!double.IsFinite(ratio) || ratio <= MinimumPositiveRatio)
        {
            return 0.0;
        }

        return Math.Clamp(ratio, 0.0, 1.0);
    }

    private sealed class BranchSourceGroup
    {
        public BranchSourceGroup(int fromBeat, IReadOnlyList<JukeboxEdge> edges)
        {
            FromBeat = fromBeat;
            Edges = edges;
            BestScore = edges.Count == 0 ? 0.0 : edges.Max(edge => edge.Similarity);
            EdgeCount = edges.Count;
        }

        public int FromBeat { get; }

        public IReadOnlyList<JukeboxEdge> Edges { get; }

        public double BestScore { get; }

        public int EdgeCount { get; }
    }
}
