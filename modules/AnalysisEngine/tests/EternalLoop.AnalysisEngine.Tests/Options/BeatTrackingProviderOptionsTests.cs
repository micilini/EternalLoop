using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Options;

public sealed class BeatTrackingProviderOptionsTests
{
    [Fact]
    public void AnalysisOptions_defaults_to_auto_provider_with_built_in_fallback()
    {
        var options = new AnalysisOptions();

        options.BeatProvider.Should().Be(BeatTrackingProviderKind.Auto);
        options.AiFallbackMode.Should().Be(AiFallbackMode.FallbackToBuiltIn);
    }

    [Fact]
    public void BeatTrackingOptions_defaults_to_auto_provider_with_built_in_fallback()
    {
        var options = new BeatTrackingOptions();

        options.BeatProvider.Should().Be(BeatTrackingProviderKind.Auto);
        options.AiFallbackMode.Should().Be(AiFallbackMode.FallbackToBuiltIn);
    }
}
