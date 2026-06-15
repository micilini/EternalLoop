using EternalLoop.BranchAnalysis.Cli.Cli;
using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Runner;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Cli;

public sealed class BranchAnalysisParserTests
{
    [Fact]
    public void ParseShouldUseDefaultsWhenArgumentsAreEmpty()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(Array.Empty<string>());

        result.Errors.Should().BeEmpty();
        result.Help.Should().BeFalse();
        result.Options.AnalysisRoot.Should().Be(BranchAnalysisDefaults.AnalysisRoot);
        result.Options.OutputRoot.Should().Be(BranchAnalysisDefaults.OutputRoot);
        result.Options.QuantumType.Should().Be("beats");
        result.Options.SimilarityThreshold.Should().Be(0.86);
        result.Options.LookaheadDepth.Should().Be(1);
        result.Options.MinJumpDistance.Should().Be(4);
        result.Options.MaxBranches.Should().Be(4);
        result.Options.MaxThreshold.Should().Be(80);
        result.Options.Force.Should().BeTrue();
        result.Options.Pretty.Should().BeTrue();
        result.Options.Quiet.Should().BeFalse();
        result.Options.StructuralPolicy.Should().BeTrue();
        result.Options.AntiLocalLoopPolicy.Should().BeTrue();
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void ParseShouldReturnHelpForHelpFlags(string helpFlag)
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse([helpFlag]);

        result.Help.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ParseShouldAcceptFullCommand()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(
        [
            "--analysis-root", @"C:\Lab\examples\2. audio-analysis",
            "--output-root", @"C:\Lab\examples\3. branchs-analysis",
            "--quantum-type", "beats",
            "--similarity-threshold", "0.92",
            "--lookahead-depth", "2",
            "--min-jump-distance", "16",
            "--max-branches", "8",
            "--max-threshold", "120",
            "--force",
            "--pretty",
            "--quiet",
            "--disable-structural-policy"
        ]);

        result.Errors.Should().BeEmpty();
        BranchAnalysisOptions options = result.Options;
        options.AnalysisRoot.Should().Be(@"C:\Lab\examples\2. audio-analysis");
        options.OutputRoot.Should().Be(@"C:\Lab\examples\3. branchs-analysis");
        options.QuantumType.Should().Be("beats");
        options.SimilarityThreshold.Should().Be(0.92);
        options.LookaheadDepth.Should().Be(2);
        options.MinJumpDistance.Should().Be(16);
        options.MaxBranches.Should().Be(8);
        options.MaxThreshold.Should().Be(120);
        options.Force.Should().BeTrue();
        options.Pretty.Should().BeTrue();
        options.Quiet.Should().BeTrue();
        options.StructuralPolicy.Should().BeFalse();
        options.AntiLocalLoopPolicy.Should().BeFalse();
    }

    [Fact]
    public void ParseShouldRejectMissingValue()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(["--analysis-root"]);

        result.Errors.Should().Contain("Missing value for --analysis-root");
    }

    [Fact]
    public void ParseShouldAcceptTuningFlags()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(
        [
            "--similarity-threshold", "0.92",
            "--lookahead-depth", "2",
            "--min-jump-distance", "16"
        ]);

        result.Errors.Should().BeEmpty();
        result.Options.SimilarityThreshold.Should().Be(0.92);
        result.Options.LookaheadDepth.Should().Be(2);
        result.Options.MinJumpDistance.Should().Be(16);
        result.Options.MaxThreshold.Should().BeLessThan(80);
    }

    [Fact]
    public void ParseShouldLetExplicitMaxThresholdOverrideSimilarityThreshold()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(
        [
            "--similarity-threshold", "0.92",
            "--max-threshold", "80"
        ]);

        result.Errors.Should().BeEmpty();
        result.Options.SimilarityThreshold.Should().Be(0.92);
        result.Options.MaxThreshold.Should().Be(80);
    }

    [Fact]
    public void ParseShouldRejectMissingValueWhenNextArgumentStartsWithDoubleDashAndContinue()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(["--analysis-root", "--quiet"]);

        result.Errors.Should().Contain("Missing value for --analysis-root");
        result.Options.Quiet.Should().BeTrue();
    }

    [Fact]
    public void ParseShouldRejectUnknownArgument()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(["--banana"]);

        result.Errors.Should().Contain("Unknown argument: --banana");
    }

    [Theory]
    [InlineData("--max-branches", "abc", "maxBranches must be a positive integer")]
    [InlineData("--max-branches", "0", "maxBranches must be a positive integer")]
    [InlineData("--max-branches", "1.5", "maxBranches must be a positive integer")]
    [InlineData("--max-threshold", "abc", "maxThreshold must be a positive integer")]
    [InlineData("--max-threshold", "-1", "maxThreshold must be a positive integer")]
    [InlineData("--lookahead-depth", "0", "lookaheadDepth must be a positive integer")]
    [InlineData("--min-jump-distance", "-1", "minJumpDistance must be a positive integer")]
    [InlineData("--similarity-threshold", "0.5", "similarityThreshold must be a number between 0.65 and 0.95")]
    [InlineData("--similarity-threshold", "abc", "similarityThreshold must be a number between 0.65 and 0.95")]
    public void ParseShouldRejectInvalidNumbers(string flag, string value, string expectedError)
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse([flag, value]);

        result.Errors.Should().Contain(expectedError);
    }

    [Fact]
    public void ParseShouldRejectUnsupportedQuantumType()
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse(["--quantum-type", "bars"]);

        result.Errors.Should().Contain("Unsupported quantum type: bars");
    }

    [Theory]
    [InlineData("--analysis-root", "", "analysisRoot cannot be empty")]
    [InlineData("--output-root", "", "outputRoot cannot be empty")]
    public void ParseShouldRejectEmptyRoots(string flag, string value, string expectedError)
    {
        BranchAnalysisParseResult result = BranchAnalysisParser.Parse([flag, value]);

        result.Errors.Should().Contain(expectedError);
    }
}
