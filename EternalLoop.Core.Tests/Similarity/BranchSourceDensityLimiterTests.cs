using EternalLoop.Contracts.Models;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class BranchSourceDensityLimiterTests
{
    [Fact]
    public void Limit_keeps_all_sources_when_under_target_limit()
    {
        var edges = CreateEdges(sourceCount: 3, edgesPerSource: 2);

        var limited = BranchSourceDensityLimiter.Limit(
            edges,
            beatCount: 40,
            maxBranchesPerBeat: 2,
            targetBranchSourceRatio: 0.20,
            maxBranchSourceRatio: 0.30);

        limited.Select(edge => edge.FromBeat).Distinct().Should().HaveCount(3);
        limited.Should().HaveCount(edges.Length);
    }

    [Fact]
    public void Limit_reduces_sources_when_above_target_limit()
    {
        var edges = CreateEdges(sourceCount: 12, edgesPerSource: 1);

        var limited = BranchSourceDensityLimiter.Limit(
            edges,
            beatCount: 50,
            maxBranchesPerBeat: 1,
            targetBranchSourceRatio: 0.10,
            maxBranchSourceRatio: 0.20);

        limited.Select(edge => edge.FromBeat).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void Limit_keeps_highest_scoring_sources()
    {
        var edges = new[]
        {
            CreateEdge(0, 20, 0.70),
            CreateEdge(1, 21, 0.95),
            CreateEdge(2, 22, 0.80),
            CreateEdge(3, 23, 0.90)
        };

        var limited = BranchSourceDensityLimiter.Limit(
            edges,
            beatCount: 20,
            maxBranchesPerBeat: 1,
            targetBranchSourceRatio: 0.10,
            maxBranchSourceRatio: 0.20);

        limited.Select(edge => edge.FromBeat).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public void Limit_preserves_max_branches_per_kept_source()
    {
        var edges = CreateEdges(sourceCount: 3, edgesPerSource: 5);

        var limited = BranchSourceDensityLimiter.Limit(
            edges,
            beatCount: 30,
            maxBranchesPerBeat: 2,
            targetBranchSourceRatio: 1.0,
            maxBranchSourceRatio: 1.0);

        limited.GroupBy(edge => edge.FromBeat).Should().OnlyContain(group => group.Count() <= 2);
    }

    [Fact]
    public void Limit_never_creates_new_edges()
    {
        var edges = CreateEdges(sourceCount: 10, edgesPerSource: 3);

        var limited = BranchSourceDensityLimiter.Limit(
            edges,
            beatCount: 60,
            maxBranchesPerBeat: 2,
            targetBranchSourceRatio: 0.10,
            maxBranchSourceRatio: 0.20);

        limited.Should().OnlyContain(edge => edges.Any(original =>
            original.FromBeat == edge.FromBeat &&
            original.ToBeat == edge.ToBeat &&
            Math.Abs(original.Similarity - edge.Similarity) < 0.000001));
    }

    [Fact]
    public void Limit_uses_max_ratio_when_target_ratio_is_invalid()
    {
        var edges = CreateEdges(sourceCount: 10, edgesPerSource: 1);

        var limited = BranchSourceDensityLimiter.Limit(
            edges,
            beatCount: 20,
            maxBranchesPerBeat: 1,
            targetBranchSourceRatio: 0.0,
            maxBranchSourceRatio: 0.25);

        limited.Select(edge => edge.FromBeat).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void Limit_returns_empty_when_ratios_are_invalid()
    {
        var edges = CreateEdges(sourceCount: 10, edgesPerSource: 1);

        var limited = BranchSourceDensityLimiter.Limit(
            edges,
            beatCount: 20,
            maxBranchesPerBeat: 1,
            targetBranchSourceRatio: 0.0,
            maxBranchSourceRatio: 0.0);

        limited.Should().BeEmpty();
    }

    private static JukeboxEdge[] CreateEdges(int sourceCount, int edgesPerSource)
    {
        var edges = new List<JukeboxEdge>();

        for (var source = 0; source < sourceCount; source++)
        {
            for (var edgeIndex = 0; edgeIndex < edgesPerSource; edgeIndex++)
            {
                edges.Add(CreateEdge(
                    source,
                    source + 100 + edgeIndex,
                    1.0 - (source * 0.01) - (edgeIndex * 0.001)));
            }
        }

        return edges.ToArray();
    }

    private static JukeboxEdge CreateEdge(int fromBeat, int toBeat, double similarity)
    {
        return new JukeboxEdge
        {
            FromBeat = fromBeat,
            ToBeat = toBeat,
            Similarity = similarity
        };
    }
}
