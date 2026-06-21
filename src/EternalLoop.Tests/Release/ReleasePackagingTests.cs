using FluentAssertions;

namespace EternalLoop.Tests.Release;

public sealed class ReleasePackagingTests
{
    [Fact]
    public void PublishProfileShouldUseRelativeWinX64SelfContainedOutput()
    {
        string content = ReadRepositoryFile("src", "EternalLoop.App", "Properties", "PublishProfiles", "win-x64-self-contained.pubxml");

        content.Should().Contain("<RuntimeIdentifier>win-x64</RuntimeIdentifier>");
        content.Should().Contain("<SelfContained>true</SelfContained>");
        content.Should().Contain("<PublishSingleFile>true</PublishSingleFile>");
        content.Should().Contain("<PublishTrimmed>false</PublishTrimmed>");
        content.Should().Contain(@"artifacts\publish\EternalLoop-1.3.0-win-x64");
    }

    [Theory]
    [InlineData(@"C:\Users")]
    [InlineData("sdanz")]
    [InlineData("Desktop")]
    [InlineData("v1.0.0")]
    [InlineData("EternalLoop-v1.0.0")]
    public void PublishProfileShouldNotContainPersonalPaths(string blockedText)
    {
        string content = ReadRepositoryFile("src", "EternalLoop.App", "Properties", "PublishProfiles", "win-x64-self-contained.pubxml");

        content.Should().NotContain(blockedText);
    }

    [Fact]
    public void AppProjectShouldDeclareReleaseVersionAndRuntime()
    {
        string content = ReadRepositoryFile("src", "EternalLoop.App", "EternalLoop.App.csproj");

        content.Should().Contain("<TargetFramework>net8.0-windows</TargetFramework>");
        content.Should().Contain("<UseWPF>true</UseWPF>");
        content.Should().Contain("<Version>1.3.0</Version>");
        content.Should().Contain("<AssemblyVersion>1.3.0.0</AssemblyVersion>");
        content.Should().Contain("<FileVersion>1.3.0.0</FileVersion>");
        content.Should().Contain("<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>");
    }

    [Fact]
    public void GitignoreShouldIgnoreArtifacts()
    {
        string content = ReadRepositoryFile(".gitignore");

        content.Should().Contain("artifacts/");
    }

    private static string ReadRepositoryFile(params string[] pathSegments)
    {
        string repositoryRoot = FindRepositoryRoot();
        string path = Path.Combine([repositoryRoot, .. pathSegments]);

        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "EternalLoop.slnx");

            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
