using EternalLoop.BranchAnalysis.Cli.Cli;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Cli;

public sealed class BranchAnalysisHelpWriterTests
{
    [Fact]
    public void BuildHelpTextShouldContainMainSections()
    {
        string text = BranchAnalysisHelpWriter.BuildHelpText();

        text.Should().Contain("EternalLoop Branch Analysis CLI");
        text.Should().Contain("EternalLoop.BranchAnalysis.Cli.exe [options]");
        text.Should().Contain("--analysis-root");
        text.Should().Contain("--output-root");
        text.Should().Contain("--quantum-type");
        text.Should().Contain("--similarity-threshold");
        text.Should().Contain("--lookahead-depth");
        text.Should().Contain("--min-jump-distance");
        text.Should().Contain("--max-branches");
        text.Should().Contain("--max-threshold");
        text.Should().Contain("--disable-structural-policy");
        text.Should().Contain("--help");
    }

    [Fact]
    public void BuildHelpTextShouldContainDefaults()
    {
        string text = BranchAnalysisHelpWriter.BuildHelpText();

        text.Should().Contain(@"..\examples\2. audio-analysis");
        text.Should().Contain(@"..\examples\3. branchs-analysis");
        text.Should().Contain("beats");
        text.Should().Contain("4");
        text.Should().Contain("80");
    }

    [Fact]
    public void BuildHelpTextShouldMentionRuntimeIsolation()
    {
        string text = BranchAnalysisHelpWriter.BuildHelpText();

        text.Should().Contain("comparison-only");
    }

    [Fact]
    public void WriteHelpShouldWriteBuildHelpText()
    {
        using StringWriter writer = new();

        BranchAnalysisHelpWriter.WriteHelp(writer);

        writer.ToString().Should().Be(BranchAnalysisHelpWriter.BuildHelpText());
    }
}
