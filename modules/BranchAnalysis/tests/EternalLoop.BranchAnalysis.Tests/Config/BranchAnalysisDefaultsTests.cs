using EternalLoop.BranchAnalysis.Core.Config;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Config;

public sealed class BranchAnalysisDefaultsTests
{
    [Fact]
    public void DefaultsShouldMatchNodeContract()
    {
        BranchAnalysisDefaults.AnalysisRoot.Should().Be(@"..\examples\2. audio-analysis");
        BranchAnalysisDefaults.OutputRoot.Should().Be(@"..\examples\3. branchs-analysis");
        BranchAnalysisDefaults.QuantumType.Should().Be("beats");
        BranchAnalysisDefaults.MaxBranches.Should().Be(4);
        BranchAnalysisDefaults.MaxThreshold.Should().Be(80);
        BranchAnalysisDefaults.StructuralPolicy.Should().BeTrue();
        BranchAnalysisDefaults.AntiLocalLoopPolicy.Should().BeTrue();
        BranchAnalysisDefaults.ShortBranchPolicy.Should().Be("structural-gated");
        BranchAnalysisDefaults.VeryShortBars.Should().Be(2);
        BranchAnalysisDefaults.ShortBars.Should().Be(4);
        BranchAnalysisDefaults.PhraseBars.Should().Be(8);
        BranchAnalysisDefaults.LocalWindowBars.Should().Be(2);
        BranchAnalysisDefaults.MaxShortLocalBranchesPerCluster.Should().Be(1);
        BranchAnalysisDefaults.Force.Should().BeTrue();
        BranchAnalysisDefaults.Pretty.Should().BeTrue();
        BranchAnalysisDefaults.Quiet.Should().BeFalse();
    }
}
