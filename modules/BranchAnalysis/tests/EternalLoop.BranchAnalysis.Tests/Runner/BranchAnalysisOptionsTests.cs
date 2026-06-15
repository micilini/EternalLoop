using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Runner;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Runner;

public sealed class BranchAnalysisOptionsTests
{
    [Fact]
    public void CreateDefaultShouldCopyAllDefaultValues()
    {
        BranchAnalysisOptions options = BranchAnalysisOptions.CreateDefault();

        options.AnalysisRoot.Should().Be(BranchAnalysisDefaults.AnalysisRoot);
        options.OutputRoot.Should().Be(BranchAnalysisDefaults.OutputRoot);
        options.QuantumType.Should().Be(BranchAnalysisDefaults.QuantumType);
        options.SimilarityThreshold.Should().Be(BranchAnalysisDefaults.SimilarityThreshold);
        options.LookaheadDepth.Should().Be(BranchAnalysisDefaults.LookaheadDepth);
        options.MinJumpDistance.Should().Be(BranchAnalysisDefaults.MinJumpDistance);
        options.MaxBranches.Should().Be(BranchAnalysisDefaults.MaxBranches);
        options.MaxThreshold.Should().Be(BranchAnalysisDefaults.MaxThreshold);
        options.StructuralPolicy.Should().Be(BranchAnalysisDefaults.StructuralPolicy);
        options.AntiLocalLoopPolicy.Should().Be(BranchAnalysisDefaults.AntiLocalLoopPolicy);
        options.ShortBranchPolicy.Should().Be(BranchAnalysisDefaults.ShortBranchPolicy);
        options.VeryShortBars.Should().Be(BranchAnalysisDefaults.VeryShortBars);
        options.ShortBars.Should().Be(BranchAnalysisDefaults.ShortBars);
        options.PhraseBars.Should().Be(BranchAnalysisDefaults.PhraseBars);
        options.LocalWindowBars.Should().Be(BranchAnalysisDefaults.LocalWindowBars);
        options.MaxShortLocalBranchesPerCluster.Should().Be(BranchAnalysisDefaults.MaxShortLocalBranchesPerCluster);
        options.Force.Should().Be(BranchAnalysisDefaults.Force);
        options.Pretty.Should().Be(BranchAnalysisDefaults.Pretty);
        options.Quiet.Should().Be(BranchAnalysisDefaults.Quiet);
    }

    [Fact]
    public void CreateDefaultShouldReturnDifferentInstances()
    {
        BranchAnalysisOptions first = BranchAnalysisOptions.CreateDefault();
        BranchAnalysisOptions second = BranchAnalysisOptions.CreateDefault();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void ChangingOneDefaultInstanceShouldNotChangeAnother()
    {
        BranchAnalysisOptions first = BranchAnalysisOptions.CreateDefault();
        BranchAnalysisOptions second = BranchAnalysisOptions.CreateDefault();

        first.MaxBranches = 99;

        second.MaxBranches.Should().Be(BranchAnalysisDefaults.MaxBranches);
    }
}
