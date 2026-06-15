using EternalLoop.BranchAnalysis.Cli.Cli;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Cli;

public sealed class BranchAnalysisExitCodesTests
{
    [Fact]
    public void ExitCodesShouldMatchNodeContract()
    {
        BranchAnalysisExitCodes.Success.Should().Be(0);
        BranchAnalysisExitCodes.InvalidArguments.Should().Be(1);
        BranchAnalysisExitCodes.AnalysisRootNotFound.Should().Be(2);
        BranchAnalysisExitCodes.NoAnalysisFilesFound.Should().Be(3);
        BranchAnalysisExitCodes.ValidationFailed.Should().Be(4);
        BranchAnalysisExitCodes.BranchAnalysisFailed.Should().Be(5);
        BranchAnalysisExitCodes.ExportFailed.Should().Be(6);
    }
}
