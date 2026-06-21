using EternalLoop.AnalysisEngine.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking;

public sealed class BeatTrackingResultProviderMetadataTests
{
    [Fact]
    public void Defaults_indicate_built_in_provider_without_ai()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5],
            Confidences = [1.0, 0.9]
        };

        result.ProviderName.Should().Be("built-in");
        result.ProviderVersion.Should().Be("analysisengine-built-in");
        result.ProviderLicense.Should().Be("MIT");
        result.ModelName.Should().Be("none");
        result.ModelSha256.Should().Be("none");
        result.UsedAiProvider.Should().BeFalse();
        result.UsedBuiltInProvider.Should().BeTrue();
        result.UsedFallbackProvider.Should().BeFalse();
        result.FallbackReason.Should().BeNull();
        result.DownbeatTimes.Should().BeEmpty();
        result.BeatNumbers.Should().BeEmpty();
        result.EstimatedMeter.Should().BeNull();
    }

    [Fact]
    public void Ai_ready_result_preserves_downbeats_beat_numbers_and_metadata()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [1.0, 0.9, 0.8, 0.7],
            DownbeatTimes = [0.0, 2.0, 4.0],
            BeatNumbers = [1, 2, 3, 4],
            EstimatedMeter = 4,
            ProviderName = "beat-this",
            ProviderVersion = "onnx-local",
            ProviderLicense = "MIT",
            ModelName = "beat-this-large",
            ModelSha256 = "abc123",
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };

        result.DownbeatTimes.Should().Equal(0.0, 2.0, 4.0);
        result.BeatNumbers.Should().Equal(1, 2, 3, 4);
        result.EstimatedMeter.Should().Be(4);
        result.ProviderName.Should().Be("beat-this");
        result.ProviderVersion.Should().Be("onnx-local");
        result.ProviderLicense.Should().Be("MIT");
        result.ModelName.Should().Be("beat-this-large");
        result.ModelSha256.Should().Be("abc123");
        result.UsedAiProvider.Should().BeTrue();
        result.UsedBuiltInProvider.Should().BeFalse();
    }
}
