using EternalLoop.Contracts.Models;
using FluentAssertions;
using System.Collections.Generic;

namespace EternalLoop.Core.Tests.Contracts;

public sealed class JukeboxGraphContractTests
{
    [Fact]
    public void JukeboxGraph_Should_AcceptEmptyEdges()
    {
        var graph = new JukeboxGraph
        {
            Nodes = Array.Empty<JukeboxNode>(),
            JumpEdges = new Dictionary<int, List<JukeboxEdge>>(),
            SimilarityThreshold = 0.85,
            LookaheadDepth = 3
        };

        graph.Nodes.Should().BeEmpty();
        graph.JumpEdges.Should().BeEmpty();
        graph.SimilarityThreshold.Should().Be(0.85);
        graph.LookaheadDepth.Should().Be(3);
    }

    [Fact]
    public void JukeboxEdge_Should_StoreJumpInformation()
    {
        var edge = new JukeboxEdge
        {
            FromBeat = 10,
            ToBeat = 42,
            Similarity = 0.91
        };

        edge.FromBeat.Should().Be(10);
        edge.ToBeat.Should().Be(42);
        edge.Similarity.Should().Be(0.91);
    }
}
