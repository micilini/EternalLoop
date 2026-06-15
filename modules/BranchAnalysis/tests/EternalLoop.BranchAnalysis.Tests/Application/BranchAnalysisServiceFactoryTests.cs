using EternalLoop.BranchAnalysis.Core.Application;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Application;

public sealed class BranchAnalysisServiceFactoryTests
{
    [Fact]
    public void CreateDefaultShouldReturnBranchAnalysisService()
    {
        var service = BranchAnalysisServiceFactory.CreateDefault();

        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IBranchAnalysisService>();
    }
}
