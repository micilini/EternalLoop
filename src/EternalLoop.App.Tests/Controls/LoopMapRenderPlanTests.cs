using System.IO;
using EternalLoop.App.Controls;
using EternalLoop.Playback.Visualization;
using FluentAssertions;

namespace EternalLoop.App.Tests.Controls;

public sealed class LoopMapRenderPlanTests
{
    [Fact]
    public void CreateShouldBuildBeatOrdinalsOnceFromGraphNodes()
    {
        BranchGraph graph = CreateGraph(
            [10, 20, 30],
            []);

        LoopMapRenderPlan plan = LoopMapRenderPlan.Create(graph);

        plan.BeatOrdinals.Should().ContainKey(10).WhoseValue.Should().Be(0);
        plan.BeatOrdinals.Should().ContainKey(20).WhoseValue.Should().Be(1);
        plan.BeatOrdinals.Should().ContainKey(30).WhoseValue.Should().Be(2);
    }

    [Fact]
    public void CreateShouldIgnoreSelfEdgesInDisplayEdges()
    {
        BranchGraphEdge selfEdge = CreateEdge(1, 1, 1, 0);
        BranchGraphEdge branchEdge = CreateEdge(2, 1, 2, 0.3);
        BranchGraph graph = CreateGraph([1, 2], [selfEdge, branchEdge]);

        LoopMapRenderPlan plan = LoopMapRenderPlan.Create(graph);

        plan.DisplayEdges.Should().ContainSingle().Which.Should().BeSameAs(branchEdge);
    }

    [Fact]
    public void CreateShouldKeepOnlyMaxDisplayedEdges()
    {
        BranchGraphEdge[] edges = Enumerable
            .Range(0, LoopMapRenderPlan.MaxDisplayedEdges + 1)
            .Select(index => CreateEdge(index, 0, index + 1, index))
            .ToArray();
        BranchGraph graph = CreateGraph(
            Enumerable.Range(0, LoopMapRenderPlan.MaxDisplayedEdges + 2),
            edges);

        LoopMapRenderPlan plan = LoopMapRenderPlan.Create(graph);

        plan.DisplayEdges.Should().HaveCount(LoopMapRenderPlan.MaxDisplayedEdges);
        plan.DisplayEdges.Should().NotContain(edge => edge.Id == LoopMapRenderPlan.MaxDisplayedEdges);
    }

    [Fact]
    public void CreateShouldPreserveBaseEdgeOrdering()
    {
        BranchGraphEdge highQuality = CreateEdge(3, 0, 1, 0);
        BranchGraphEdge mediumQuality = CreateEdge(1, 0, 2, 1);
        BranchGraphEdge lowQuality = CreateEdge(2, 0, 3, 3);
        BranchGraph graph = CreateGraph([0, 1, 2, 3], [highQuality, mediumQuality, lowQuality]);

        LoopMapRenderPlan plan = LoopMapRenderPlan.Create(graph);

        plan.DisplayEdges.Should().Equal(lowQuality, mediumQuality, highQuality);
    }

    [Fact]
    public void CreateShouldPreserveHighlightedEdgeOrdering()
    {
        BranchGraphEdge third = CreateEdge(30, 4, 7, 1);
        BranchGraphEdge second = CreateEdge(20, 4, 6, 0);
        BranchGraphEdge first = CreateEdge(10, 4, 5, 0);
        BranchGraphEdge otherBeat = CreateEdge(40, 3, 1, 0);
        BranchGraph graph = CreateGraph([1, 3, 4, 5, 6, 7], [third, second, first, otherBeat]);

        LoopMapRenderPlan plan = LoopMapRenderPlan.Create(graph);

        plan.TryGetHighlightedEdges(4, out IReadOnlyList<BranchGraphEdge> edges).Should().BeTrue();
        edges.Should().Equal(first, second, third);
    }

    [Fact]
    public void TryGetHighlightedEdgesShouldReturnFalseWhenBeatHasNoEdges()
    {
        BranchGraph graph = CreateGraph([0, 1], [CreateEdge(1, 0, 1, 0)]);

        LoopMapRenderPlan plan = LoopMapRenderPlan.Create(graph);

        plan.TryGetHighlightedEdges(1, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetHighlightedEdgesShouldReturnAtMostTwelveEdges()
    {
        BranchGraphEdge[] edges = Enumerable
            .Range(0, LoopMapRenderPlan.HighlightedEdgeCount + 4)
            .Select(index => CreateEdge(index, 2, index + 3, 0))
            .ToArray();
        BranchGraph graph = CreateGraph(Enumerable.Range(0, 24), edges);

        LoopMapRenderPlan plan = LoopMapRenderPlan.Create(graph);

        plan.TryGetHighlightedEdges(2, out IReadOnlyList<BranchGraphEdge> highlightedEdges).Should().BeTrue();
        highlightedEdges.Should().HaveCount(LoopMapRenderPlan.HighlightedEdgeCount);
    }

    [Fact]
    public void EmptyShouldHaveNoOrdinalsOrEdges()
    {
        LoopMapRenderPlan.Empty.BeatOrdinals.Should().BeEmpty();
        LoopMapRenderPlan.Empty.DisplayEdges.Should().BeEmpty();
        LoopMapRenderPlan.Empty.TryGetHighlightedEdges(0, out _).Should().BeFalse();
    }

    [Fact]
    public void LoopMapVisualizationShouldUseRenderPlanCache()
    {
        string content = ReadControlFile();

        content.Should().Contain("_cachedGraph");
        content.Should().Contain("_renderPlan");
        content.Should().Contain("GetRenderPlan");
        content.Should().Contain("LoopMapRenderPlan plan = GetRenderPlan(graph);");
        content.Should().NotContain("CreateBeatOrdinals(graph)");
        content.Should().NotContain("EnumerateDisplayEdges(graph)");
    }

    private static BranchGraph CreateGraph(
        IEnumerable<int> beatIndexes,
        IReadOnlyList<BranchGraphEdge> edges)
    {
        BranchGraphNode[] nodes = beatIndexes
            .Select(beatIndex => new BranchGraphNode { BeatIndex = beatIndex })
            .ToArray();

        return new BranchGraph
        {
            Nodes = nodes,
            Edges = edges,
            TotalBeatCount = nodes.Length,
            DisplayedEdgeCount = edges.Count,
            HiddenEdgeCount = 0
        };
    }

    private static BranchGraphEdge CreateEdge(
        int id,
        int fromBeat,
        int toBeat,
        double distance)
    {
        return new BranchGraphEdge
        {
            Id = id,
            FromBeat = fromBeat,
            ToBeat = toBeat,
            Distance = distance
        };
    }

    private static string ReadControlFile()
    {
        string repositoryRoot = FindRepositoryRoot();
        string path = Path.Combine(
            repositoryRoot,
            "src",
            "EternalLoop.App",
            "Controls",
            "LoopMapVisualization.cs");

        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "EternalLoop.slnx");

            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
