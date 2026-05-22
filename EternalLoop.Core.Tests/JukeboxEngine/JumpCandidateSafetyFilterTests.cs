using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.JukeboxEngine;
using FluentAssertions;

namespace EternalLoop.Core.Tests.JukeboxEngine;

public sealed class JumpCandidateSafetyFilterTests
{
    [Fact]
    public void IsCandidateSafe_ReturnsTrue_ForNormalDestination()
    {
        var options = new JukeboxEngineOptions
        {
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 12
        };

        var safe = JumpCandidateSafetyFilter.IsCandidateSafe(
            10,
            Edge(10, 40),
            CreateGraph(100, Edge(60, 20)),
            100,
            options);

        safe.Should().BeTrue();
    }

    [Fact]
    public void IsCandidateSafe_ReturnsFalse_ForTerminalDestinationWithoutEscape()
    {
        var options = new JukeboxEngineOptions
        {
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 12
        };

        var safe = JumpCandidateSafetyFilter.IsCandidateSafe(
            10,
            Edge(10, 94),
            CreateGraph(100),
            100,
            options);

        safe.Should().BeFalse();
    }

    [Fact]
    public void IsCandidateSafe_ReturnsTrue_ForTerminalDestinationWithEscape()
    {
        var options = new JukeboxEngineOptions
        {
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 12,
            TerminalEscapeLookaheadBeats = 8
        };
        var graph = CreateGraph(100, Edge(96, 24), Edge(60, 20));

        var safe = JumpCandidateSafetyFilter.IsCandidateSafe(
            10,
            Edge(10, 94),
            graph,
            100,
            options);

        safe.Should().BeTrue();
    }

    [Fact]
    public void IsInEndGuardZone_DetectsFinalRegion()
    {
        var options = new JukeboxEngineOptions
        {
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 11
        };

        JumpCandidateSafetyFilter.IsInEndGuardZone(87, 100, options).Should().BeFalse();
        JumpCandidateSafetyFilter.IsInEndGuardZone(88, 100, options).Should().BeTrue();
        JumpCandidateSafetyFilter.IsInEndGuardZone(99, 100, options).Should().BeTrue();
    }

    [Fact]
    public void IsCandidateSafe_ReturnsFalse_ForOutOfRangeDestination()
    {
        var graph = CreateGraph(100);

        JumpCandidateSafetyFilter.IsCandidateSafe(10, Edge(10, -1), graph, 100, new JukeboxEngineOptions())
            .Should().BeFalse();
        JumpCandidateSafetyFilter.IsCandidateSafe(10, Edge(10, 100), graph, 100, new JukeboxEngineOptions())
            .Should().BeFalse();
    }

    [Fact]
    public void IsCandidateSafe_ReturnsFalse_ForIntermediateDestinationThatReachesEndWithoutEscape()
    {
        var safe = JumpCandidateSafetyFilter.IsCandidateSafe(
            75,
            Edge(75, 80),
            CreateGraph(100),
            100,
            CreateRouteOptions());

        safe.Should().BeFalse();
    }

    [Fact]
    public void IsCandidateSafe_ReturnsTrue_ForIntermediateDestinationWithRealEscapeBeforeEnd()
    {
        var graph = CreateGraph(100, Edge(84, 30));

        var safe = JumpCandidateSafetyFilter.IsCandidateSafe(
            75,
            Edge(75, 80),
            graph,
            100,
            CreateRouteOptions());

        safe.Should().BeTrue();
    }

    [Fact]
    public void IsCandidateSafe_ReturnsFalse_WhenChainOnlyPushesTowardTerminal()
    {
        var graph = CreateGraph(100, Edge(84, 94), Edge(96, 98));

        var safe = JumpCandidateSafetyFilter.IsCandidateSafe(
            75,
            Edge(75, 80),
            graph,
            100,
            CreateRouteOptions());

        safe.Should().BeFalse();
    }

    [Fact]
    public void IsCandidateSafe_ReturnsTrue_WhenTerminalChainEscapesToSafeRoute()
    {
        var graph = CreateGraph(100, Edge(84, 94), Edge(96, 30), Edge(60, 20));

        var safe = JumpCandidateSafetyFilter.IsCandidateSafe(
            75,
            Edge(75, 80),
            graph,
            100,
            CreateRouteOptions());

        safe.Should().BeTrue();
    }

    [Fact]
    public void IsLastSafeExitBeforeTerminal_ReturnsTrue_WhenNoFutureSafeCandidateExists()
    {
        var graph = CreateGraph(100, Edge(84, 30));

        var isLastExit = JumpCandidateSafetyFilter.IsLastSafeExitBeforeTerminal(
            84,
            graph,
            100,
            CreateRouteOptions());

        isLastExit.Should().BeTrue();
    }

    [Fact]
    public void IsLastSafeExitBeforeTerminal_ReturnsFalse_WhenFutureSafeCandidateExists()
    {
        var graph = CreateGraph(100, Edge(80, 30), Edge(84, 20));

        var isLastExit = JumpCandidateSafetyFilter.IsLastSafeExitBeforeTerminal(
            80,
            graph,
            100,
            CreateRouteOptions());

        isLastExit.Should().BeFalse();
    }

    private static JukeboxGraph CreateGraph(int beatCount, params JukeboxEdge[] edges)
    {
        var nodes = Enumerable.Range(0, beatCount)
            .Select(index => new JukeboxNode
            {
                BeatIndex = index,
                Start = index * 0.5,
                Duration = 0.5
            })
            .ToArray();

        return new JukeboxGraph
        {
            Nodes = nodes,
            JumpEdges = edges
                .GroupBy(edge => edge.FromBeat)
                .ToDictionary(group => group.Key, group => group.ToList()),
            SimilarityThreshold = 0.85,
            LookaheadDepth = 3
        };
    }

    private static JukeboxEdge Edge(int from, int to)
    {
        return new JukeboxEdge
        {
            FromBeat = from,
            ToBeat = to,
            Similarity = 1.0
        };
    }

    private static JukeboxEngineOptions CreateRouteOptions()
    {
        return new JukeboxEngineOptions
        {
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 12,
            TerminalEscapeLookaheadBeats = 8
        };
    }
}
