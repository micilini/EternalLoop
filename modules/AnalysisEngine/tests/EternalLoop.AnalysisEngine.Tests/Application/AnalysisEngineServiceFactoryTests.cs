using EternalLoop.AnalysisEngine.Core.Application;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Application;

public sealed class AnalysisEngineServiceFactoryTests
{
    [Fact]
    public void CreateDefaultShouldReturnAnalysisEngineService()
    {
        var service = AnalysisEngineServiceFactory.CreateDefault();

        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IAnalysisEngineService>();
    }
}
