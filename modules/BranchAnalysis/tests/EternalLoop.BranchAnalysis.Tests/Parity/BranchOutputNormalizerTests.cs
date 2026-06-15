using System.Text.Json.Nodes;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Parity;

public sealed class BranchOutputNormalizerTests
{
    [Fact]
    public void NormalizeShouldNeutralizeVolatileFieldsSortBranchesAndRoundNumbers()
    {
        JsonNode output = CreateOutput("2026-01-01T00:00:00Z", "node://source", reverseBranches: true);

        JsonNode normalized = BranchOutputNormalizer.Normalize(output);

        normalized["exportedAt"]!.GetValue<string>().Should().Be("<normalized-exportedAt>");
        normalized["sourcePage"]!.GetValue<string>().Should().Be("<normalized-sourcePage>");
        normalized["activeBranches"]![0]!["fromBeat"]!.GetValue<int>().Should().Be(1);
        normalized["activeBranches"]![0]!["distance"]!.GetValue<double>().Should().Be(1.123457);
        normalized["counts"]!["activeBranches"]!.GetValue<int>().Should().Be(2);
        normalized["policy"].Should().NotBeNull();
        normalized["candidateBranches"].Should().NotBeNull();
    }

    internal static JsonNode CreateOutput(string exportedAt, string sourcePage, bool reverseBranches = false)
    {
        JsonArray activeBranches = reverseBranches
            ?
            [
                CreateBranch(2, 4, 1, 2.1234561),
                CreateBranch(1, 1, 3, 1.1234567)
            ]
            :
            [
                CreateBranch(1, 1, 3, 1.1234567),
                CreateBranch(2, 4, 1, 2.1234561)
            ];

        return new JsonObject
        {
            ["schemaVersion"] = "eternalloop-branch-export-v1",
            ["exportedAt"] = exportedAt,
            ["sourcePage"] = sourcePage,
            ["branchSource"] = "track.analysis.beats[*].neighbors",
            ["track"] = new JsonObject
            {
                ["id"] = "track-a",
                ["title"] = "Track A"
            },
            ["tuning"] = new JsonObject
            {
                ["quantumType"] = "beats"
            },
            ["policy"] = new JsonObject
            {
                ["name"] = "structural-branch-utility-v1"
            },
            ["counts"] = new JsonObject
            {
                ["activeBranches"] = 2,
                ["candidateBranches"] = 1
            },
            ["diagnostics"] = new JsonObject
            {
                ["structurallyRejectedBranches"] = 0
            },
            ["activeBranches"] = activeBranches,
            ["candidateBranches"] = new JsonArray
            {
                CreateBranch(3, 8, 4, 3)
            }
        };
    }

    private static JsonObject CreateBranch(int id, int fromBeat, int toBeat, double distance)
    {
        return new JsonObject
        {
            ["id"] = id,
            ["status"] = "active",
            ["fromBeat"] = fromBeat,
            ["toBeat"] = toBeat,
            ["jumpBeats"] = toBeat - fromBeat,
            ["direction"] = toBeat < fromBeat ? "backward" : "forward",
            ["distance"] = distance,
            ["deleted"] = false,
            ["quality"] = new JsonObject
            {
                ["acousticDistance"] = distance,
                ["branchScore"] = distance + 0.0000001,
                ["thresholdGate"] = "acoustic-distance",
                ["policyReasons"] = new JsonArray { "same-bar-phase" }
            },
            ["source"] = CreateQuantum(fromBeat),
            ["destination"] = CreateQuantum(toBeat)
        };
    }

    private static JsonObject CreateQuantum(int which)
    {
        return new JsonObject
        {
            ["which"] = which,
            ["start"] = which,
            ["duration"] = 1,
            ["confidence"] = 1,
            ["indexInParent"] = which % 4,
            ["overlappingSegmentCount"] = 1,
            ["overlappingSegments"] = new JsonArray
            {
                new JsonObject
                {
                    ["which"] = which,
                    ["start"] = which + 0.1,
                    ["duration"] = 0.2,
                    ["confidence"] = 1,
                    ["loudness_start"] = -10,
                    ["loudness_max"] = -2,
                    ["loudness_max_time"] = 0.05
                }
            }
        };
    }
}
