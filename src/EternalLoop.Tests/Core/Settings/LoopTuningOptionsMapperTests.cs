using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Settings;

public sealed class LoopTuningOptionsMapperTests
{
    [Fact]
    public void ToAnalysisOptionsShouldEnableMusicalQualityForBalanced()
    {
        var settings = LoopTuningSettings.Balanced();

        AnalysisOptions options = LoopTuningOptionsMapper.ToAnalysisOptions(settings);

        options.Artist.Should().Be("Local");
        options.MusicalQuality.AcousticSegmentation.Should().BeTrue();
        options.MusicalQuality.BeatMicroSnap.Should().BeTrue();
        options.MusicalQuality.AdaptiveTatums.Should().BeTrue();
        options.MusicalQuality.StructuralSections.Should().BeTrue();
        options.MusicalQuality.EvidenceConfidences.Should().BeTrue();
    }

    [Fact]
    public void ToBranchAnalysisOptionsShouldMapBalancedScriptDefaults()
    {
        var settings = LoopTuningSettings.Balanced();

        var options = LoopTuningOptionsMapper.ToBranchAnalysisOptions(
            settings,
            force: true,
            pretty: true,
            quiet: true);

        options.QuantumType.Should().Be("beats");
        options.MaxBranches.Should().Be(6);
        options.MaxThreshold.Should().Be(80);
        options.SimilarityThreshold.Should().Be(0.86);
        options.LookaheadDepth.Should().Be(1);
        options.MinJumpDistance.Should().Be(4);
        options.Force.Should().BeTrue();
        options.Pretty.Should().BeTrue();
        options.Quiet.Should().BeTrue();
    }

    [Fact]
    public void ToBranchAnalysisOptionsShouldMapSimilarityToEffectiveThreshold()
    {
        LoopTuningSettings conservative = LoopTuningSettings.Balanced();
        conservative.SimilarityThreshold = 0.92;

        LoopTuningSettings wild = LoopTuningSettings.Balanced();
        wild.SimilarityThreshold = 0.78;

        BranchAnalysis.Core.Runner.BranchAnalysisOptions strictOptions =
            LoopTuningOptionsMapper.ToBranchAnalysisOptions(
                conservative,
                force: true,
                pretty: true,
                quiet: true);

        BranchAnalysis.Core.Runner.BranchAnalysisOptions wildOptions =
            LoopTuningOptionsMapper.ToBranchAnalysisOptions(
                wild,
                force: true,
                pretty: true,
                quiet: true);

        strictOptions.MaxThreshold.Should().BeLessThan(80);
        wildOptions.MaxThreshold.Should().BeGreaterThan(80);
    }
}
