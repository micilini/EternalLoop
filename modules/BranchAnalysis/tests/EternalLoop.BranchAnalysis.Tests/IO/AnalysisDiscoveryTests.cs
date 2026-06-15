using EternalLoop.BranchAnalysis.Core.IO;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.IO;

public sealed class AnalysisDiscoveryTests
{
    [Fact]
    public void DiscoveryShouldFindAnalysisFilesOneLevelBelowRoot()
    {
        string root = CreateTempDirectory();
        string songDirectory = Path.Combine(root, "1-gangnam-style");
        Directory.CreateDirectory(songDirectory);
        File.WriteAllText(Path.Combine(songDirectory, AnalysisDiscovery.AnalysisFileName), "{}");

        IReadOnlyList<AnalysisDiscoveryResult> results = AnalysisDiscovery.Discover(root);

        results.Should().ContainSingle();
        results[0].Name.Should().Be("1-gangnam-style");
        results[0].AnalysisPath.Should().EndWith(AnalysisDiscovery.AnalysisFileName);
        Directory.Exists(results[0].DirectoryPath).Should().BeTrue();
        Path.IsPathFullyQualified(results[0].DirectoryPath).Should().BeTrue();
        Path.IsPathFullyQualified(results[0].AnalysisPath).Should().BeTrue();
    }

    [Fact]
    public void DiscoveryShouldIgnoreFoldersWithoutAnalysisFile()
    {
        string root = CreateTempDirectory();
        string validDirectory = Path.Combine(root, "valid-song");
        string invalidDirectory = Path.Combine(root, "invalid-song");
        Directory.CreateDirectory(validDirectory);
        Directory.CreateDirectory(invalidDirectory);
        File.WriteAllText(Path.Combine(validDirectory, AnalysisDiscovery.AnalysisFileName), "{}");
        File.WriteAllText(Path.Combine(invalidDirectory, "outro.json"), "{}");

        IReadOnlyList<AnalysisDiscoveryResult> results = AnalysisDiscovery.Discover(root);

        results.Should().ContainSingle();
        results[0].Name.Should().Be("valid-song");
    }

    [Fact]
    public void DiscoveryShouldSortResultsByFolderName()
    {
        string root = CreateTempDirectory();
        CreateAnalysisFolder(root, "b-song");
        CreateAnalysisFolder(root, "a-song");

        IReadOnlyList<AnalysisDiscoveryResult> results = AnalysisDiscovery.Discover(root);

        results.Select(result => result.Name).Should().Equal("a-song", "b-song");
    }

    [Fact]
    public void DiscoveryShouldThrowWhenRootDoesNotExist()
    {
        string root = Path.Combine(Path.GetTempPath(), $"eternalloop-missing-{Guid.NewGuid():N}");

        Action act = () => AnalysisDiscovery.Discover(root);

        act.Should().Throw<AnalysisRootNotFoundException>()
            .WithMessage($"Analysis root does not exist: {Path.GetFullPath(root)}");
    }

    [Fact]
    public void DiscoveryShouldThrowWhenRootIsFile()
    {
        string root = CreateTempDirectory();
        string filePath = Path.Combine(root, "analysis-root-file.json");
        File.WriteAllText(filePath, "{}");

        Action act = () => AnalysisDiscovery.Discover(filePath);

        act.Should().Throw<AnalysisRootNotFoundException>()
            .WithMessage($"Analysis root does not exist: {Path.GetFullPath(filePath)}");
    }

    [Fact]
    public void DiscoveryShouldNotSearchRecursively()
    {
        string root = CreateTempDirectory();
        string nestedDirectory = Path.Combine(root, "song-a", "nested");
        Directory.CreateDirectory(nestedDirectory);
        File.WriteAllText(Path.Combine(nestedDirectory, AnalysisDiscovery.AnalysisFileName), "{}");

        IReadOnlyList<AnalysisDiscoveryResult> results = AnalysisDiscovery.Discover(root);

        results.Should().BeEmpty();
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateTempSubdirectory("eternalloop-branch-discovery-").FullName;
    }

    private static void CreateAnalysisFolder(string root, string name)
    {
        string directory = Path.Combine(root, name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, AnalysisDiscovery.AnalysisFileName), "{}");
    }
}
