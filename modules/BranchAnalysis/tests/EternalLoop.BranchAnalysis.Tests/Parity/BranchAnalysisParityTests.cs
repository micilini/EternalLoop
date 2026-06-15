using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.IO;
using EternalLoop.BranchAnalysis.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Parity;

public sealed class BranchAnalysisParityTests
{
    private const string TrackName = "parity-song-a";

    [Fact]
    public void CSharpBranchOutputShouldBeComparableAfterNormalization()
    {
        string analysisRoot = CreateTempDirectory();
        string outputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteParityAnalysisFile(analysisRoot, TrackName);

        string outputPath = CSharpBranchAnalysisRunner.Run(analysisRoot, outputRoot, TrackName);
        JsonNode first = BranchAnalysisJsonReader.Read(outputPath);
        JsonNode second = first.DeepClone();

        BranchOutputComparisonResult result = BranchOutputComparator.Compare(first, second);

        result.AreEqual.Should().BeTrue(result.ToFailureMessage());
    }

    [Fact]
    public void NodeAndCSharpShouldGenerateEquivalentBranchOutputWhenParityIsEnabled()
    {
        if (!IsNodeParityEnabled())
        {
            return;
        }

        string solutionRoot = SolutionRootLocator.Locate();
        NodeBranchAnalysisRunner nodeRunner = new(solutionRoot);

        if (!nodeRunner.IsAvailable)
        {
            return;
        }

        string analysisRoot = CreateTempDirectory();
        string nodeOutputRoot = CreateTempDirectory();
        string csharpOutputRoot = CreateTempDirectory();
        AnalysisFixtureFactory.WriteParityAnalysisFile(analysisRoot, TrackName);

        string nodeOutputPath = nodeRunner.Run(analysisRoot, nodeOutputRoot, TrackName);
        string csharpOutputPath = CSharpBranchAnalysisRunner.Run(analysisRoot, csharpOutputRoot, TrackName);
        JsonNode nodeOutput = BranchAnalysisJsonReader.Read(nodeOutputPath);
        JsonNode csharpOutput = BranchAnalysisJsonReader.Read(csharpOutputPath);

        BranchOutputComparisonResult result = BranchOutputComparator.Compare(nodeOutput, csharpOutput);

        result.AreEqual.Should().BeTrue(
            $"{result.ToFailureMessage()}{Environment.NewLine}Node JSON: {nodeOutputPath}{Environment.NewLine}CSharp JSON: {csharpOutputPath}");
    }

    private static bool IsNodeParityEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("ETERNALLOOP_RUN_NODE_PARITY"),
            "1",
            StringComparison.Ordinal);
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateTempSubdirectory("eternalloop-branch-parity-").FullName;
    }
}
