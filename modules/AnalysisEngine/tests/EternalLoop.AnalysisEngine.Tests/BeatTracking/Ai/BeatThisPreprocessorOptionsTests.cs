using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class BeatThisPreprocessorOptionsTests
{
    [Fact]
    public void Defaults_match_beat_this_contract()
    {
        var options = new BeatThisPreprocessorOptions();

        options.SampleRate.Should().Be(22_050);
        options.FrameRate.Should().Be(50.0);
        options.ChunkFrames.Should().Be(1_500);
        options.MelBins.Should().Be(128);
        options.FrameSize.Should().Be(1_024);
        options.LogEpsilon.Should().BeGreaterThan(0.0);
        options.Normalize.Should().BeTrue();
    }

    [Fact]
    public void FromMetadata_uses_model_contract_fields()
    {
        var metadata = new BeatThisModelMetadata
        {
            SampleRate = 44_100,
            FrameRate = 50.0,
            ChunkFrames = 777,
            MelBins = 96,
            FrameSize = 2_048
        };

        var options = BeatThisPreprocessorOptions.FromMetadata(metadata);

        options.SampleRate.Should().Be(44_100);
        options.FrameRate.Should().Be(50.0);
        options.ChunkFrames.Should().Be(777);
        options.MelBins.Should().Be(96);
        options.FrameSize.Should().Be(2_048);
    }

    [Fact]
    public void Default_frame_rate_is_50fps()
    {
        var options = new BeatThisPreprocessorOptions();

        options.FrameRate.Should().Be(50.0);
    }

    [Fact]
    public void PreprocessorOptions_FromMetadata_preserves_50fps()
    {
        var options = BeatThisPreprocessorOptions.FromMetadata(new BeatThisModelMetadata());

        options.FrameRate.Should().Be(50.0);
    }
}
