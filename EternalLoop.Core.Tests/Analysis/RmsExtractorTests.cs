using EternalLoop.Core.Analysis;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Analysis;

public sealed class RmsExtractorTests
{
    [Fact]
    public void Compute_Should_ReturnEmpty_WhenSamplesShorterThanFrame()
    {
        var rms = RmsExtractor.Compute(new float[100], frameSize: 2048, hopLength: 512);

        rms.Should().BeEmpty();
    }

    [Fact]
    public void Compute_Should_ReturnZero_ForSilentSignal()
    {
        var samples = new float[4096];

        var rms = RmsExtractor.Compute(samples, frameSize: 2048, hopLength: 512);

        rms.Should().NotBeEmpty();
        rms.Should().AllSatisfy(value => value.Should().Be(0f));
    }

    [Fact]
    public void Compute_Should_ReturnConstant_ForConstantDcOffset()
    {
        var samples = new float[4096];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = 0.5f;
        }

        var rms = RmsExtractor.Compute(samples, frameSize: 2048, hopLength: 512);

        rms.Should().NotBeEmpty();
        rms.Should().AllSatisfy(value => value.Should().BeApproximately(0.5f, 1e-4f));
    }

    [Fact]
    public void Compute_Should_ReturnRoughlyHalf_ForSineWaveOfAmplitudeOne()
    {
        var samples = new float[8192];
        var frequency = 440.0;
        var sampleRate = 22_050.0;

        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)Math.Sin(2.0 * Math.PI * frequency * i / sampleRate);
        }

        var rms = RmsExtractor.Compute(samples, frameSize: 2048, hopLength: 512);

        rms.Should().NotBeEmpty();
        rms.Should().AllSatisfy(value => value.Should().BeApproximately(0.7071f, 0.05f));
    }

    [Fact]
    public void Compute_Should_ProduceCorrectFrameCount()
    {
        var samples = new float[10_000];

        var rms = RmsExtractor.Compute(samples, frameSize: 2048, hopLength: 512);

        var expectedFrameCount = ((10_000 - 2048) / 512) + 1;
        rms.Length.Should().Be(expectedFrameCount);
    }

    [Fact]
    public void Compute_Should_Throw_WhenFrameSizeNotPositive()
    {
        var act = () => RmsExtractor.Compute(new float[1000], frameSize: 0, hopLength: 512);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Compute_Should_Throw_WhenHopLengthNotPositive()
    {
        var act = () => RmsExtractor.Compute(new float[1000], frameSize: 2048, hopLength: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
