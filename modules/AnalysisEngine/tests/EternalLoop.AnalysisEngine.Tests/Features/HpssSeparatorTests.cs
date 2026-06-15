using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Features;

public sealed class HpssSeparatorTests
{
    [Fact]
    public void SeparatesWithoutNaN()
    {
        var frames = BuildFrames();

        var result = HpssSeparator.Separate(frames, new HpssOptions { UseHpss = true });

        Values(result.PercussiveFrames).Concat(Values(result.HarmonicFrames))
            .Should()
            .OnlyContain(value => float.IsFinite(value));
    }

    [Fact]
    public void PreservesFrameCount()
    {
        var frames = BuildFrames();

        var result = HpssSeparator.Separate(frames, new HpssOptions { UseHpss = true });

        result.PercussiveFrames.Should().HaveCount(frames.Length);
        result.HarmonicFrames.Should().HaveCount(frames.Length);
    }

    [Fact]
    public void ProducesPercussiveAndHarmonicEnergy()
    {
        var frames = BuildFrames();

        var result = HpssSeparator.Separate(frames, new HpssOptions { UseHpss = true });

        result.PercussiveEnergyRatio.Should().BeGreaterThan(0.0);
        result.HarmonicEnergyRatio.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void HandlesShortAudio()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 0.05);
        var frames = StftAnalyzer.Analyze(audio.Samples, 2048, 512);

        var result = HpssSeparator.Separate(frames, new HpssOptions { UseHpss = true });

        result.PercussiveFrames.Should().HaveCount(frames.Length);
        result.HarmonicFrames.Should().HaveCount(frames.Length);
    }

    [Fact]
    public void IsDeterministic()
    {
        var frames = BuildFrames();
        var options = new HpssOptions { UseHpss = true };

        var first = HpssSeparator.Separate(frames, options);
        var second = HpssSeparator.Separate(frames, options);

        Values(first.PercussiveFrames).Should().Equal(Values(second.PercussiveFrames));
        Values(first.HarmonicFrames).Should().Equal(Values(second.HarmonicFrames));
    }

    private static StftFrame[] BuildFrames()
    {
        var audio = TestSignalFactory.CreateSineLoadedAudio(durationSeconds: 0.5, frequency: 220.0);
        var samples = audio.Samples.ToArray();
        for (var index = 0; index < samples.Length; index += 2205)
        {
            samples[index] += 0.8f;
        }

        return StftAnalyzer.Analyze(samples, 2048, 512);
    }

    private static IEnumerable<float> Values(IEnumerable<StftFrame> frames)
    {
        return frames.SelectMany(frame => frame.Magnitudes).Concat(frames.SelectMany(frame => frame.PowerSpectrum));
    }
}
