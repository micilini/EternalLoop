using EternalLoop.Core.Workflow;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Workflow;

public sealed class TrackInputTests
{
    [Fact]
    public void FromFilePathShouldNormalizePathAndExposeFileName()
    {
        var input = TrackInput.FromFilePath(@"music\track.mp3");

        input.FilePath.Should().EndWith(Path.Combine("music", "track.mp3"));
        input.FileName.Should().Be("track.mp3");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void FromFilePathShouldRejectEmptyPath(string filePath)
    {
        var act = () => TrackInput.FromFilePath(filePath);

        act.Should().Throw<ArgumentException>();
    }
}
