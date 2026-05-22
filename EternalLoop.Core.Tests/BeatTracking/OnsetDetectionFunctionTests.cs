using EternalLoop.Core.BeatTracking;
using FluentAssertions;

namespace EternalLoop.Core.Tests.BeatTracking;

public sealed class OnsetDetectionFunctionTests
{
    [Fact]
    public void Build_Should_ReturnEmpty_WhenInputIsEmpty()
    {
        var odf = OnsetDetectionFunction.Build([], 7);

        odf.Should().BeEmpty();
    }

    [Fact]
    public void Build_Should_ClampNegativeValues()
    {
        var odf = OnsetDetectionFunction.Build([-1f, -0.5f, 0f], 1);

        odf.Should().OnlyContain(value => value >= 0f);
    }

    [Fact]
    public void Build_Should_PreserveLength()
    {
        var odf = OnsetDetectionFunction.Build([0f, 2f, 4f, 2f, 0f], 1);

        odf.Should().HaveCount(5);
    }

    [Fact]
    public void Build_Should_NormalizeMaximumToOne()
    {
        var odf = OnsetDetectionFunction.Build([0f, 2f, 4f, 2f, 0f], 1);

        odf.Max().Should().BeApproximately(1.0f, 0.0001f);
    }

    [Fact]
    public void Build_Should_ReturnZeros_WhenInputIsAllZeros()
    {
        var odf = OnsetDetectionFunction.Build([0f, 0f, 0f], 7);

        odf.Should().OnlyContain(value => value == 0f);
    }
}
