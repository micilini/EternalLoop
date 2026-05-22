using EternalLoop.Contracts.Models;
using EternalLoop.Core.JukeboxEngine;
using FluentAssertions;

namespace EternalLoop.Core.Tests.JukeboxEngine;

public sealed class JumpDecisionPolicyTests
{
    [Fact]
    public void ChooseLeastPlayed_Selects_Destination_With_Lowest_Play_Count_Sum()
    {
        var candidates = new[] { Edge(0, 2, 0.9), Edge(0, 4, 0.8) };
        var playCounts = new[] { 0, 0, 5, 5, 0, 0 };

        var choice = JumpDecisionPolicy.ChooseLeastPlayed(candidates, playCounts, 2);

        choice.Should().Be(4);
    }

    [Fact]
    public void ChooseLeastPlayed_Prefers_LessTerminalDestination_BeforeSimilarity_WhenPlayCountsTie()
    {
        var candidates = new[] { Edge(0, 2, 0.7), Edge(0, 4, 0.9) };
        var playCounts = new[] { 0, 0, 0, 0, 0, 0 };

        var choice = JumpDecisionPolicy.ChooseLeastPlayed(candidates, playCounts, 1);

        choice.Should().Be(2);
    }

    [Fact]
    public void ChooseLeastPlayed_Prefers_LessTerminalDestination_InLargeTrackTie()
    {
        var candidates = new[] { Edge(75, 30, 0.8), Edge(75, 82, 0.95) };
        var playCounts = new int[100];

        var choice = JumpDecisionPolicy.ChooseLeastPlayed(candidates, playCounts, 1, beatCount: 100);

        choice.Should().Be(30);
    }

    [Fact]
    public void ChooseHighestSimilarity_Selects_Highest_Similarity()
    {
        var choice = JumpDecisionPolicy.ChooseHighestSimilarity([Edge(0, 2, 0.7), Edge(0, 4, 0.9)]);

        choice.Should().Be(4);
    }

    [Fact]
    public void ChooseWeighted_Returns_A_Valid_Candidate()
    {
        var candidates = new[] { Edge(0, 2, 0.7), Edge(0, 4, 0.9) };

        var choice = JumpDecisionPolicy.ChooseWeighted(candidates, new Random(123));

        choice.Should().BeOneOf(2, 4);
    }

    [Fact]
    public void ChooseRandom_Returns_A_Valid_Candidate()
    {
        var candidates = new[] { Edge(0, 2, 0.7), Edge(0, 4, 0.9) };

        var choice = JumpDecisionPolicy.ChooseRandom(candidates, new Random(123));

        choice.Should().BeOneOf(2, 4);
    }

    [Fact]
    public void Policies_Throw_When_Candidates_Are_Empty()
    {
        var candidates = Array.Empty<JukeboxEdge>();

        FluentActions.Invoking(() => JumpDecisionPolicy.ChooseHighestSimilarity(candidates)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => JumpDecisionPolicy.ChooseRandom(candidates, new Random(1))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => JumpDecisionPolicy.ChooseWeighted(candidates, new Random(1))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => JumpDecisionPolicy.ChooseLeastPlayed(candidates, [0], 1)).Should().Throw<ArgumentException>();
    }

    private static JukeboxEdge Edge(int from, int to, double similarity)
    {
        return new JukeboxEdge
        {
            FromBeat = from,
            ToBeat = to,
            Similarity = similarity
        };
    }
}
