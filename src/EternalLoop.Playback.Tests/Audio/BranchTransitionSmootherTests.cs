using EternalLoop.Playback.Audio;
using FluentAssertions;

namespace EternalLoop.Playback.Tests.Audio;

public sealed class BranchTransitionSmootherTests
{
    [Fact]
    public void NormalizeShouldClampInvalidFade()
    {
        var smoother = new BranchTransitionSmoother(
            sampleRate: 1000,
            channels: 1,
            new BranchTransitionOptions
            {
                FadeMilliseconds = double.NaN,
                MinFadeMilliseconds = 1,
                MaxFadeMilliseconds = 12
            });

        smoother.FadeFrames.Should().Be(8);
    }

    [Fact]
    public void ApplyInputGainShouldReduceStartOfBranchJump()
    {
        var smoother = new BranchTransitionSmoother(1000, 1, new BranchTransitionOptions { FadeMilliseconds = 10 });

        smoother.ApplyInputGain(1, 0, BranchTransitionKind.BranchJump).Should().Be(0);
        smoother.ApplyInputGain(1, 5, BranchTransitionKind.BranchJump).Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void ApplyOutputGainShouldReduceEndOfBeat()
    {
        var smoother = new BranchTransitionSmoother(1000, 1, new BranchTransitionOptions { FadeMilliseconds = 10 });

        smoother.ApplyOutputGain(1, 5).Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void DisabledSmootherShouldKeepSamplesUnchanged()
    {
        var smoother = new BranchTransitionSmoother(1000, 1, new BranchTransitionOptions { Enabled = false });

        smoother.ApplyInputGain(1, 0, BranchTransitionKind.BranchJump).Should().Be(1);
        smoother.ApplyOutputGain(1, 0).Should().Be(1);
    }

    [Fact]
    public void SmootherShouldNotProduceNaN()
    {
        var smoother = new BranchTransitionSmoother(1000, 1, new BranchTransitionOptions { FadeMilliseconds = 10 });

        smoother.ApplyInputGain(float.NaN, 1, BranchTransitionKind.BranchJump).Should().Be(0);
        smoother.ApplyOutputGain(float.NaN, 1).Should().Be(0);
    }

    [Fact]
    public void SmootherShouldNotProduceInfinity()
    {
        var smoother = new BranchTransitionSmoother(1000, 1, new BranchTransitionOptions { FadeMilliseconds = 10 });

        smoother.ApplyInputGain(float.PositiveInfinity, 1, BranchTransitionKind.BranchJump).Should().Be(0);
        smoother.ApplyOutputGain(float.NegativeInfinity, 1).Should().Be(0);
    }

    [Fact]
    public void SmootherShouldHandleShortBuffers()
    {
        var smoother = new BranchTransitionSmoother(1000, 1, new BranchTransitionOptions { FadeMilliseconds = 100 });

        float input = smoother.ApplyInputGain(1, 1, BranchTransitionKind.BranchJump);
        float output = smoother.ApplyOutputGain(1, 1);

        input.Should().BeGreaterThan(0).And.BeLessThan(1);
        output.Should().BeGreaterThan(0).And.BeLessThan(1);
        float.IsFinite(input).Should().BeTrue();
        float.IsFinite(output).Should().BeTrue();
    }

    [Fact]
    public void SmootherShouldHandleMonoAndStereo()
    {
        var mono = new BranchTransitionSmoother(1000, 1, new BranchTransitionOptions { FadeMilliseconds = 10 });
        var stereo = new BranchTransitionSmoother(1000, 2, new BranchTransitionOptions { FadeMilliseconds = 10 });

        mono.FadeSamples.Should().Be(10);
        stereo.FadeSamples.Should().Be(20);
        mono.ApplyInputGain(1, 5, BranchTransitionKind.BranchJump).Should().BeApproximately(0.5f, 0.001f);
        stereo.ApplyInputGain(1, 5, BranchTransitionKind.BranchJump).Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void FadeLengthLongerThanBufferShouldNotCrash()
    {
        var smoother = new BranchTransitionSmoother(44100, 2, new BranchTransitionOptions { FadeMilliseconds = 500 });

        Action act = () =>
        {
            _ = smoother.ApplyInputGain(0.25f, 2, BranchTransitionKind.BranchJump);
            _ = smoother.ApplyOutputGain(0.25f, 2);
        };

        act.Should().NotThrow();
    }
}
