using EternalLoop.AnalysisEngine.Cli;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Cli;

public sealed class AnalysisEngineParserHybridCalibrationTests
{
    [Theory]
    [InlineData("strict", HybridCalibrationProfile.StrictProduction)]
    [InlineData("strict-production", HybridCalibrationProfile.StrictProduction)]
    [InlineData("production", HybridCalibrationProfile.StrictProduction)]
    public void Parser_accepts_strict_production(string value, HybridCalibrationProfile expected)
    {
        Parse(value).Arguments!.HybridCalibrationProfile.Should().Be(expected);
    }

    [Theory]
    [InlineData("balanced", HybridCalibrationProfile.BalancedProbe)]
    [InlineData("balanced-probe", HybridCalibrationProfile.BalancedProbe)]
    [InlineData("probe", HybridCalibrationProfile.BalancedProbe)]
    public void Parser_accepts_balanced_probe_aliases(string value, HybridCalibrationProfile expected)
    {
        Parse(value).Arguments!.HybridCalibrationProfile.Should().Be(expected);
    }

    [Theory]
    [InlineData("exploratory", HybridCalibrationProfile.ExploratoryProbe)]
    [InlineData("exploratory-probe", HybridCalibrationProfile.ExploratoryProbe)]
    [InlineData("aggressive", HybridCalibrationProfile.ExploratoryProbe)]
    public void Parser_accepts_exploratory_probe_aliases(string value, HybridCalibrationProfile expected)
    {
        Parse(value).Arguments!.HybridCalibrationProfile.Should().Be(expected);
    }

    [Fact]
    public void Parser_rejects_invalid_hybrid_calibration_profile()
    {
        var result = Parse("invalid");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid hybrid calibration profile. Expected strict-production, balanced-probe, or exploratory-probe.");
    }

    private static AnalysisEngineParseResult Parse(string profile)
    {
        return AnalysisEngineParser.Parse([
            "--input", "song.wav",
            "--output-dir", "out",
            "--hybrid-calibration-profile", profile
        ]);
    }
}
