using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Export;
using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Preprocessing;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Contract;

public sealed class BranchOutputContractTests
{
    [Fact]
    public void BranchWriterShouldWriteBranchExportContract()
    {
        string outputRoot = CreateTempDirectory();
        (TrackAnalysisDocument track, BranchGraphData data) = CreateContractFixture();
        BranchExportPayload payload = BranchExportPayloadBuilder.Build(track, data);

        BranchAnalysisWriteResult result = BranchAnalysisWriter.Write(
            outputRoot,
            "export-fixture",
            payload,
            new BranchAnalysisWriteOptions { Force = true, Pretty = true });

        Path.GetFileName(result.OutputPath).Should().Be(BranchAnalysisWriter.BranchFileName);
        JsonNode root = BranchAnalysisJsonReader.Read(result.OutputPath);
        root["schemaVersion"]!.GetValue<string>().Should().Be(BranchExportPayloadBuilder.BranchExportSchemaVersion);
        root["sourcePage"]!.GetValue<string>().Should().Be(BranchExportPayloadBuilder.SourcePage);
        root["branchSource"]!.GetValue<string>().Should().Be("track.analysis.beats[*].neighbors");
        root["activeBranches"].Should().BeOfType<JsonArray>();
        root["candidateBranches"].Should().BeOfType<JsonArray>();

        JsonArray activeBranches = root["activeBranches"]!.AsArray();
        JsonArray candidateBranches = root["candidateBranches"]!.AsArray();
        root["counts"]!["activeBranches"]!.GetValue<int>().Should().Be(activeBranches.Count);
        root["counts"]!["candidateBranches"]!.GetValue<int>().Should().Be(candidateBranches.Count);
        activeBranches.Should().NotBeEmpty();
        candidateBranches.Should().NotBeEmpty();

        AssertBranchShape(activeBranches[0]!);
        AssertBranchShape(candidateBranches[0]!);

        string json = root.ToJsonString();
        json.Should().NotContain("pitches");
        json.Should().NotContain("timbre");
    }

    [Fact]
    public void BranchExportShouldBeDeterministicAfterExportedAtNormalization()
    {
        (TrackAnalysisDocument firstTrack, BranchGraphData firstData) = CreateContractFixture();
        (TrackAnalysisDocument secondTrack, BranchGraphData secondData) = CreateContractFixture();
        BranchExportPayload firstPayload = BranchExportPayloadBuilder.Build(firstTrack, firstData);
        BranchExportPayload secondPayload = BranchExportPayloadBuilder.Build(secondTrack, secondData);
        JsonNode first = BranchExportPayloadBuilder.ToJsonNode(firstPayload);
        JsonNode second = BranchExportPayloadBuilder.ToJsonNode(secondPayload);
        first["exportedAt"] = "stable";
        second["exportedAt"] = "stable";

        second.ToJsonString().Should().Be(first.ToJsonString());
    }

    private static void AssertBranchShape(JsonNode branch)
    {
        branch["id"].Should().NotBeNull();
        branch["status"].Should().NotBeNull();
        branch["fromBeat"].Should().NotBeNull();
        branch["toBeat"].Should().NotBeNull();
        branch["jumpBeats"].Should().NotBeNull();
        branch["direction"].Should().NotBeNull();
        branch["distance"].Should().NotBeNull();
        branch["quality"].Should().NotBeNull();
        branch["deleted"].Should().NotBeNull();
        branch["source"].Should().NotBeNull();
        branch["destination"].Should().NotBeNull();

        JsonNode source = branch["source"]!;
        source["which"].Should().NotBeNull();
        source["start"].Should().NotBeNull();
        source["duration"].Should().NotBeNull();
        source["confidence"].Should().NotBeNull();
        source["indexInParent"].Should().NotBeNull();
        source["overlappingSegmentCount"].Should().NotBeNull();
        source["overlappingSegments"].Should().BeOfType<JsonArray>();
    }

    private static (TrackAnalysisDocument Track, BranchGraphData Data) CreateContractFixture()
    {
        TrackAnalysisDocument track = new()
        {
            Info = new TrackInfo
            {
                Id = "contract-fixture",
                Title = "Contract Fixture",
                Name = "Contract Fixture",
                Artist = "Local Artist"
            },
            AudioSummary = new AudioSummary { Duration = 8 },
            Analysis = new AnalysisData
            {
                Sections = [Quantum(0, 8)],
                Bars = [Quantum(0, 4), Quantum(4, 4)],
                Beats =
                [
                    Quantum(0, 1),
                    Quantum(1, 1),
                    Quantum(2, 1),
                    Quantum(3, 1),
                    Quantum(4, 1),
                    Quantum(5, 1),
                    Quantum(6, 1),
                    Quantum(7, 1)
                ],
                Tatums =
                [
                    Quantum(0, 1),
                    Quantum(1, 1),
                    Quantum(2, 1),
                    Quantum(3, 1),
                    Quantum(4, 1),
                    Quantum(5, 1),
                    Quantum(6, 1),
                    Quantum(7, 1)
                ],
                Segments =
                [
                    Segment(0.1, 0.2, [1, 0, 0], [1, 0]),
                    Segment(1.1, 0.2, [2, 0, 0], [0, 1]),
                    Segment(2.1, 0.2, [3, 0, 0], [1, 0]),
                    Segment(3.1, 0.2, [4, 0, 0], [0, 1]),
                    Segment(4.1, 0.2, [1.1, 0, 0], [1, 0]),
                    Segment(5.1, 0.2, [2.1, 0, 0], [0, 1]),
                    Segment(6.1, 0.2, [3.1, 0, 0], [1, 0]),
                    Segment(7.1, 0.2, [4.1, 0, 0], [0, 1])
                ]
            }
        };

        TrackPreprocessor.Preprocess(track);

        BranchEdge active = Edge(track.Analysis.Beats[4], track.Analysis.Beats[0], 10, 20);
        BranchEdge candidate = Edge(track.Analysis.Beats[5], track.Analysis.Beats[1], 11, 25);
        track.Analysis.Beats[4].Neighbors = [active];
        track.Analysis.Beats[4].AllNeighbors = [active];
        track.Analysis.Beats[5].AllNeighbors = [candidate];

        BranchGraphData data = NearestNeighborCalculator.CreateBranchGraphData(track);
        data.CurrentThreshold = 30;
        data.ComputedThreshold = 30;
        data.BranchCount = 1;
        data.StructuralContext = StructuralBranchPolicy.BuildStructuralBranchContext(track);

        return (track, data);
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateTempSubdirectory("eternalloop-branch-export-").FullName;
    }

    private static TimeQuantum Quantum(double start, double duration)
    {
        return new TimeQuantum
        {
            Start = start,
            Duration = duration,
            Confidence = 1
        };
    }

    private static SegmentQuantum Segment(double start, double duration, List<double> timbre, List<double> pitches)
    {
        return new SegmentQuantum
        {
            Start = start,
            Duration = duration,
            Confidence = 1,
            LoudnessStart = -10,
            LoudnessMax = -2,
            LoudnessMaxTime = 0.05,
            Timbre = timbre,
            Pitches = pitches
        };
    }

    private static BranchEdge Edge(TimeQuantum source, TimeQuantum destination, int id, double distance)
    {
        return new BranchEdge
        {
            Id = id,
            Source = source,
            Destination = destination,
            Distance = distance,
            AcousticDistance = distance,
            BranchScore = distance + 1,
            StructuralPenalty = 1,
            StructuralBonusDiagnosticOnly = 4,
            JumpBeatsAbs = Math.Abs(destination.Which - source.Which),
            JumpBars = Math.Abs(destination.Which - source.Which) / 4.0,
            SameBarPhase = true,
            SamePhrasePhase4 = true,
            SamePhrasePhase8 = true,
            SamePhrasePhase16 = true,
            PolicyDecision = "accepted",
            PolicyReasons = ["same-bar-phase"]
        };
    }
}
