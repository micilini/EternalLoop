using EternalLoop.Contracts.Models;

namespace EternalLoop.Core.JukeboxEngine;

internal static class JumpDecisionPolicy
{
    private const double MinimumWeightedJumpScore = 0.0001;

    public static int ChooseLeastPlayed(
        IReadOnlyList<JukeboxEdge> candidates,
        IReadOnlyList<int> playCounts,
        int lookaheadDepth)
    {
        return ChooseLeastPlayed(candidates, playCounts, lookaheadDepth, playCounts?.Count ?? 0);
    }

    public static int ChooseLeastPlayed(
        IReadOnlyList<JukeboxEdge> candidates,
        IReadOnlyList<int> playCounts,
        int lookaheadDepth,
        int beatCount)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(playCounts);

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        }

        var depth = Math.Max(1, lookaheadDepth);

        return candidates
            .OrderBy(edge => ScoreDestination(edge.ToBeat, playCounts, depth))
            .ThenBy(edge => DestinationTerminalRisk(edge.ToBeat, beatCount))
            .ThenByDescending(edge => edge.Similarity)
            .ThenBy(edge => edge.ToBeat)
            .First()
            .ToBeat;
    }

    public static int ChooseRandom(
        IReadOnlyList<JukeboxEdge> candidates,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(random);

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        }

        return candidates[random.Next(candidates.Count)].ToBeat;
    }

    public static int ChooseWeighted(
        IReadOnlyList<JukeboxEdge> candidates,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(random);

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        }

        var totalWeight = candidates.Sum(edge => Math.Max(MinimumWeightedJumpScore, edge.Similarity));
        var pick = random.NextDouble() * totalWeight;
        var cumulative = 0.0;

        foreach (var edge in candidates)
        {
            cumulative += Math.Max(MinimumWeightedJumpScore, edge.Similarity);
            if (pick <= cumulative)
            {
                return edge.ToBeat;
            }
        }

        return candidates[^1].ToBeat;
    }

    public static int ChooseHighestSimilarity(IReadOnlyList<JukeboxEdge> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        }

        return candidates
            .OrderByDescending(edge => edge.Similarity)
            .ThenBy(edge => edge.ToBeat)
            .First()
            .ToBeat;
    }

    private static int ScoreDestination(
        int destinationBeat,
        IReadOnlyList<int> playCounts,
        int lookaheadDepth)
    {
        var score = 0;

        for (var i = 0; i < lookaheadDepth; i++)
        {
            var index = destinationBeat + i;
            if (index >= playCounts.Count)
            {
                break;
            }

            score += playCounts[index];
        }

        return score;
    }

    private static double DestinationTerminalRisk(int destinationBeat, int beatCount)
    {
        return destinationBeat / (double)Math.Max(1, beatCount - 1);
    }
}
