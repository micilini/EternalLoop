using EternalLoop.Core;
using FluentAssertions;

namespace EternalLoop.Tests.Core;

public sealed class CoreAssemblyMarkerTests
{
    [Fact]
    public void NameShouldIdentifyCoreAssembly()
    {
        CoreAssemblyMarker.Name.Should().Be("EternalLoop.Core");
    }
}
