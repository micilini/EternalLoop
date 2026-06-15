using EternalLoop.Playback.Models;
using EternalLoop.Playback.Runtime;
using EternalLoop.Playback.Tests.Fixtures;
using EternalLoop.Playback.Visualization;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Visualization;

public sealed class BranchGraphBuilderTests
{
    [Fact]
    public void BuildShouldReturnEmptyGraphForNullTrack()
    {
        BranchGraph graph = new BranchGraphBuilder().Build(null);

        graph.Nodes.Should().BeEmpty();
        graph.Edges.Should().BeEmpty();
        graph.TotalBeatCount.Should().Be(0);
    }

    [Fact]
    public void BuildShouldReturnEmptyGraphForEmptyTrack()
    {
        var track = new RuntimeTrack
        {
            Id = "empty",
            Title = "Empty",
            Artist = "Local",
            AudioPath = "empty.wav",
            DurationSeconds = 0,
            Beats = []
        };

        BranchGraph graph = new BranchGraphBuilder().Build(track);

        graph.Should().BeSameAs(BranchGraph.Empty);
    }

    [Fact]
    public void BuildShouldCreateOneNodePerRuntimeBeat()
    {
        var track = PlaybackFixtures.BuildTrack();

        BranchGraph graph = new BranchGraphBuilder().Build(track);

        graph.Nodes.Should().HaveCount(track.Beats.Count);
        graph.Nodes.Select(node => node.BeatIndex).Should().Equal(0, 1, 2, 3, 4);
        graph.Nodes.Should().OnlyContain(node => double.IsFinite(node.AngleRadians));
    }

    [Fact]
    public void BuildShouldCreateEdgesForActiveBranches()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);

        BranchGraph graph = new BranchGraphBuilder().Build(track);

        graph.Edges.Should().ContainSingle();
        graph.Edges[0].FromBeat.Should().Be(1);
        graph.Edges[0].ToBeat.Should().Be(3);
        graph.Edges[0].Distance.Should().Be(12);
    }

    [Fact]
    public void BuildShouldIgnoreDeletedSelfAndInvalidBranches()
    {
        var track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3),
            PlaybackFixtures.Branch(id: 2, fromBeat: 1, toBeat: 1),
            PlaybackFixtures.Branch(id: 3, fromBeat: 1, toBeat: 99),
            PlaybackFixtures.Branch(id: 4, fromBeat: 2, toBeat: 4, deleted: true)
        ]);

        BranchGraph graph = new BranchGraphBuilder().Build(track);

        graph.Edges.Should().ContainSingle(edge => edge.Id == 1);
    }

    [Fact]
    public void BuildShouldIgnoreEdgesWithInvalidDistance()
    {
        RuntimeTrack track = CreateTrackWithManualEdges(
        [
            new ManualEdge(1, 1, 3, 4),
            new ManualEdge(2, 2, 4, double.NaN)
        ]);

        BranchGraph graph = new BranchGraphBuilder().Build(track);

        graph.Edges.Should().ContainSingle(edge => edge.Id == 1);
    }

    [Fact]
    public void BuildShouldMarkCurrentBeatLastJumpEndpointsAndLastJumpEdge()
    {
        var track = PlaybackFixtures.BuildTrack([PlaybackFixtures.Branch()]);

        BranchGraph graph = new BranchGraphBuilder().Build(
            track,
            new BranchGraphOptions
            {
                CurrentBeatIndex = 3,
                LastJumpFromBeat = 1,
                LastJumpToBeat = 3
            });

        graph.Nodes.Single(node => node.BeatIndex == 3).IsCurrent.Should().BeTrue();
        graph.Nodes.Single(node => node.BeatIndex == 1).IsLastJumpEndpoint.Should().BeTrue();
        graph.Nodes.Single(node => node.BeatIndex == 3).IsLastJumpEndpoint.Should().BeTrue();
        graph.Edges.Single().IsLastJump.Should().BeTrue();
    }

    [Fact]
    public void BuildShouldLimitDisplayedEdgesAndReportHiddenEdgeCount()
    {
        RuntimeBranchInput[] branches = Enumerable.Range(0, 700)
            .Select(index => PlaybackFixtures.Branch(id: index, fromBeat: index % 2, toBeat: 3 + (index % 2), distance: index + 1))
            .ToArray();
        var track = PlaybackFixtures.BuildTrack(branches);

        BranchGraph graph = new BranchGraphBuilder().Build(track, new BranchGraphOptions { MaxDisplayedEdges = 650 });

        graph.Edges.Should().HaveCount(650);
        graph.DisplayedEdgeCount.Should().Be(650);
        graph.HiddenEdgeCount.Should().Be(50);
    }

    [Fact]
    public void PreferLowDistanceEdgesShouldOrderLowerDistanceFirst()
    {
        var track = PlaybackFixtures.BuildTrack(
        [
            PlaybackFixtures.Branch(id: 1, fromBeat: 1, toBeat: 3, distance: 20),
            PlaybackFixtures.Branch(id: 2, fromBeat: 2, toBeat: 4, distance: 2),
            PlaybackFixtures.Branch(id: 3, fromBeat: 0, toBeat: 2, distance: 8)
        ]);

        BranchGraph graph = new BranchGraphBuilder().Build(track, new BranchGraphOptions { PreferLowDistanceEdges = true });

        graph.Edges.Select(edge => edge.Id).Should().Equal(2, 3, 1);
    }

    private static RuntimeTrack CreateTrackWithManualEdges(IReadOnlyList<ManualEdge> edges)
    {
        RuntimeBeat[] beats =
        [
            new RuntimeBeat { Which = 0, Start = 0, Duration = 1, Confidence = 1 },
            new RuntimeBeat { Which = 1, Start = 1, Duration = 1, Confidence = 1 },
            new RuntimeBeat { Which = 2, Start = 2, Duration = 1, Confidence = 1 },
            new RuntimeBeat { Which = 3, Start = 3, Duration = 1, Confidence = 1 },
            new RuntimeBeat { Which = 4, Start = 4, Duration = 1, Confidence = 1 }
        ];

        for (int index = 0; index < beats.Length; index++)
        {
            beats[index].Prev = index > 0 ? beats[index - 1] : null;
            beats[index].Next = index < beats.Length - 1 ? beats[index + 1] : null;
        }

        foreach (ManualEdge edge in edges)
        {
            RuntimeBeat source = beats.Single(beat => beat.Which == edge.FromBeat);
            RuntimeBeat destination = beats.Single(beat => beat.Which == edge.ToBeat);
            source.Neighbors.Add(new RuntimeBranchEdge
            {
                Id = edge.Id,
                Status = "active",
                FromBeat = edge.FromBeat,
                ToBeat = edge.ToBeat,
                JumpBeats = edge.ToBeat - edge.FromBeat,
                Direction = edge.ToBeat >= edge.FromBeat ? "forward" : "backward",
                Distance = edge.Distance,
                Deleted = false,
                SourceBeat = source,
                DestinationBeat = destination
            });
        }

        return new RuntimeTrack
        {
            Id = "manual",
            Title = "Manual",
            Artist = "Local",
            AudioPath = "manual.wav",
            DurationSeconds = 5,
            Beats = beats
        };
    }

    private sealed record ManualEdge(int Id, int FromBeat, int ToBeat, double Distance);
}
