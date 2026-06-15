using System.Text.Json;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Validation;
using EternalLoop.BranchAnalysis.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Models;

public sealed class AnalysisModelsTests
{
    [Fact]
    public void TrackAnalysisDocumentShouldDeserializeRootFields()
    {
        TrackAnalysisDocument document = AnalysisContractValidator.ReadValidated(
            AnalysisFixtureFactory.CreateValidAnalysisNode());

        document.Info.Service.Should().Be("LOCAL");
        document.Info.Id.Should().Be("fixture-track");
        document.Info.Title.Should().Be("Fixture Track");
        document.Info.Name.Should().Be("Fixture Track");
        document.Info.Artist.Should().Be("Local");
        document.Info.Url.Should().Be("local://fixture-track.mp3");
        document.Info.Duration.Should().Be(8000);
        document.AudioSummary.Duration.Should().Be(8);
    }

    [Fact]
    public void AnalysisDataShouldDeserializeQuantumCollections()
    {
        TrackAnalysisDocument document = AnalysisContractValidator.ReadValidated(
            AnalysisFixtureFactory.CreateValidAnalysisNode());

        document.Analysis.Sections.Should().HaveCount(1);
        document.Analysis.Bars.Should().HaveCount(2);
        document.Analysis.Beats.Should().HaveCount(8);
        document.Analysis.Tatums.Should().HaveCount(8);
        document.Analysis.Segments.Should().HaveCount(8);
    }

    [Fact]
    public void SegmentQuantumShouldDeserializeLoudnessPitchAndTimbre()
    {
        TrackAnalysisDocument document = AnalysisContractValidator.ReadValidated(
            AnalysisFixtureFactory.CreateValidAnalysisNode());

        SegmentQuantum segment = document.Analysis.Segments[0];

        segment.Start.Should().Be(0.1);
        segment.Duration.Should().Be(0.2);
        segment.Confidence.Should().Be(1);
        segment.LoudnessStart.Should().Be(0);
        segment.LoudnessMax.Should().Be(0);
        segment.LoudnessMaxTime.Should().Be(0);
        segment.Pitches.Should().HaveCount(12);
        segment.Timbre.Should().HaveCount(26);
    }

    [Fact]
    public void TimeQuantumRuntimeFieldsShouldStartEmpty()
    {
        TimeQuantum quantum = new();

        quantum.Track.Should().BeNull();
        quantum.Which.Should().Be(-1);
        quantum.Prev.Should().BeNull();
        quantum.Next.Should().BeNull();
        quantum.Parent.Should().BeNull();
        quantum.Children.Should().BeEmpty();
        quantum.IndexInParent.Should().Be(-1);
        quantum.Oseg.Should().BeNull();
        quantum.OverlappingSegments.Should().BeEmpty();
        quantum.Neighbors.Should().BeEmpty();
        quantum.AllNeighbors.Should().BeEmpty();
    }

    [Fact]
    public void TimeQuantumEndShouldReturnStartPlusDuration()
    {
        TimeQuantum quantum = new()
        {
            Start = 2.5,
            Duration = 1.25
        };

        quantum.End.Should().Be(3.75);
    }

    [Fact]
    public void RuntimeFieldsShouldNotBeSerialized()
    {
        TimeQuantum quantum = new()
        {
            Start = 1,
            Duration = 2,
            Confidence = 0.5,
            Track = new TrackAnalysisDocument(),
            Which = 7,
            Prev = new TimeQuantum(),
            Next = new TimeQuantum(),
            Parent = new TimeQuantum(),
            Children = [new TimeQuantum()],
            IndexInParent = 3,
            Oseg = new SegmentQuantum(),
            OverlappingSegments = [new SegmentQuantum()],
            Neighbors = [new BranchEdge()],
            AllNeighbors = [new BranchEdge()]
        };

        string json = JsonSerializer.Serialize(quantum);

        json.Should().Contain("\"start\"");
        json.Should().Contain("\"duration\"");
        json.Should().Contain("\"confidence\"");
        json.Should().NotContain("Track");
        json.Should().NotContain("Which");
        json.Should().NotContain("Prev");
        json.Should().NotContain("Next");
        json.Should().NotContain("Parent");
        json.Should().NotContain("Children");
        json.Should().NotContain("IndexInParent");
        json.Should().NotContain("Oseg");
        json.Should().NotContain("OverlappingSegments");
        json.Should().NotContain("Neighbors");
        json.Should().NotContain("AllNeighbors");
        json.Should().NotContain("End");
    }
}
