using System.Text.Json.Nodes;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Parity;

public sealed class BranchOutputComparatorTests
{
    [Fact]
    public void BranchOutputComparatorShouldTreatEquivalentOutputsAsEqual()
    {
        JsonNode node = BranchOutputNormalizerTests.CreateOutput("2026-01-01T00:00:00Z", "node://source", reverseBranches: true);
        JsonNode csharp = BranchOutputNormalizerTests.CreateOutput("2026-06-04T00:00:00Z", "csharp://source", reverseBranches: false);
        csharp["activeBranches"]![0]!["distance"] = 1.1234568;

        BranchOutputComparisonResult result = BranchOutputComparator.Compare(node, csharp);

        result.AreEqual.Should().BeTrue(result.ToFailureMessage());
    }

    [Fact]
    public void BranchOutputComparatorShouldReportMeaningfulDifferences()
    {
        JsonNode node = BranchOutputNormalizerTests.CreateOutput("2026-01-01T00:00:00Z", "node://source");
        JsonNode csharp = node.DeepClone();
        csharp["counts"]!["activeBranches"] = 40;
        csharp["activeBranches"]![0]!["fromBeat"] = 99;

        BranchOutputComparisonResult result = BranchOutputComparator.Compare(node, csharp);

        result.AreEqual.Should().BeFalse();
        result.ToFailureMessage().Should().Contain("counts.activeBranches differs. Node=2 CSharp=40");
        result.ToFailureMessage().Should().Contain("activeBranches[0].fromBeat differs.");
    }
}
