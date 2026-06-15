using EternalLoop.AnalysisEngine.Core.Application;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Application;

public sealed class AnalysisEngineRequestTests
{
    [Fact]
    public void ConstructorShouldNormalizeInputPathAndUseDefaultOptions()
    {
        var request = new AnalysisEngineRequest(@"music\track.mp3");

        request.InputPath.Should().EndWith(Path.Combine("music", "track.mp3"));
        request.Options.Should().NotBeNull();
    }

    [Fact]
    public void ConstructorShouldPreserveExplicitOptions()
    {
        var options = new AnalysisOptions
        {
            Artist = "Test Artist"
        };

        var request = new AnalysisEngineRequest("track.mp3", options);

        request.Options.Should().BeSameAs(options);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorShouldRejectEmptyInputPath(string inputPath)
    {
        var act = () => new AnalysisEngineRequest(inputPath);

        act.Should().Throw<ArgumentException>();
    }
}
