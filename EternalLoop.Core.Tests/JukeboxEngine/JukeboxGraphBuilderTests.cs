using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.JukeboxEngine;
using FluentAssertions;

namespace EternalLoop.Core.Tests.JukeboxEngine;

public sealed class JukeboxGraphBuilderTests
{
    [Fact]
    public void Build_Creates_Node_For_Each_Beat()
    {
        var graph = JukeboxGraphBuilder.Build(CreateBeats(3), [], new BranchFindingOptions());

        graph.Nodes.Should().HaveCount(3);
    }

    [Fact]
    public void Build_Groups_Edges_By_FromBeat()
    {
        var graph = JukeboxGraphBuilder.Build(
            CreateBeats(4),
            [CreateEdge(0, 2, 0.9), CreateEdge(0, 3, 0.8)],
            new BranchFindingOptions());

        graph.JumpEdges[0].Should().HaveCount(2);
    }

    [Fact]
    public void Build_Drops_Edges_With_Invalid_Beat_Index()
    {
        var graph = JukeboxGraphBuilder.Build(
            CreateBeats(3),
            [CreateEdge(0, 2, 0.9), CreateEdge(0, 9, 0.99)],
            new BranchFindingOptions());

        graph.JumpEdges[0].Should().ContainSingle();
    }

    [Fact]
    public void Build_Sorts_Edges_By_Similarity_Descending()
    {
        var graph = JukeboxGraphBuilder.Build(
            CreateBeats(4),
            [CreateEdge(0, 2, 0.7), CreateEdge(0, 3, 0.9)],
            new BranchFindingOptions());

        graph.JumpEdges[0].Select(edge => edge.ToBeat).Should().Equal(3, 2);
    }

    [Fact]
    public void Build_Preserves_Threshold_And_Lookahead()
    {
        var options = new BranchFindingOptions
        {
            SimilarityThreshold = 0.77,
            LookaheadDepth = 4
        };

        var graph = JukeboxGraphBuilder.Build(CreateBeats(3), [], options);

        graph.SimilarityThreshold.Should().Be(0.77);
        graph.LookaheadDepth.Should().Be(4);
    }

    private static Beat[] CreateBeats(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new Beat
            {
                Index = index,
                Start = index * 0.5,
                Duration = 0.5,
                Confidence = 1.0,
                Timbre = [1f],
                Pitches = [1f],
                Loudness = [0f, 0f, 0f],
                BarPosition = [0f, 1f]
            })
            .ToArray();
    }

    private static JukeboxEdge CreateEdge(int from, int to, double similarity)
    {
        return new JukeboxEdge
        {
            FromBeat = from,
            ToBeat = to,
            Similarity = similarity
        };
    }
}
