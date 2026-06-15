using FluentAssertions;

namespace EternalLoop.Tests.Release;

public sealed class RepositoryHygieneTests
{
    [Fact]
    public void LicenseShouldExistAndUseMit()
    {
        string content = ReadRepositoryFile("LICENSE");

        content.Should().Contain("MIT License");
        content.Should().Contain("Copyright (c) 2026 William Lima");
        content.Should().NotContain("TODO");
        content.Should().NotContain("confirm before release");
    }

    [Fact]
    public void GitAttributesShouldNormalizeTextAndMarkBinaries()
    {
        string content = ReadRepositoryFile(".gitattributes");

        content.Should().Contain("* text=auto");
        content.Should().Contain("*.cs text eol=crlf");
        content.Should().Contain("*.md text eol=lf");
        content.Should().Contain("*.png binary");
        content.Should().Contain("*.mp3 binary");
        content.Should().Contain("*.zip binary");
    }

    [Fact]
    public void ReadmeShouldDescribeCurrentRelease()
    {
        string content = ReadRepositoryFile("README.md");

        content.Should().Contain("EternalLoop 1.2.0");
        content.Should().Contain("local-first");
        content.Should().Contain("MP3");
        content.Should().Contain("WAV");
        content.Should().Contain("M4A");
        content.Should().Contain("AAC");
        content.Should().Contain(@"powershell -ExecutionPolicy Bypass -File .\tools\publish-release-win-x64.ps1");
        content.Should().Contain("MIT License");
        content.Should().NotContain("V1 monorepo structure is complete");
        content.Should().NotContain("does not yet integrate the WPF app");
    }

    [Fact]
    public void ReadmeShouldNotReferenceDeletedDocs()
    {
        string content = ReadRepositoryFile("README.md");

        content.Should().NotContain("docs/");
        content.Should().NotContain(@"docs\");
        content.Should().NotContain("V2_STARTING_POINT");
        content.Should().NotContain("MONOREPO_STRUCTURE");
        content.Should().NotContain("V1_COMPLETION_CHECKLIST");
    }

    [Fact]
    public void RepositoryShouldNotContainLegacyPlanningArtifacts()
    {
        string repositoryRoot = FindRepositoryRoot();

        Directory.EnumerateFiles(repositoryRoot, "*HARD" + "ENING_1.2.0.md", SearchOption.TopDirectoryOnly)
            .Should()
            .BeEmpty();
        Directory.Exists(Path.Combine(repositoryRoot, "docs")).Should().BeFalse();
        Directory.Exists(Path.Combine(repositoryRoot, "older-files")).Should().BeFalse();
    }

    [Fact]
    public void SolutionFilesShouldNotReferenceDeletedDocsFolder()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] solutionFiles =
        [
            "EternalLoop.slnx",
            Path.Combine("modules", "AnalysisEngine", "EternalLoop.AnalysisEngine.slnx"),
            Path.Combine("modules", "BranchAnalysis", "EternalLoop.BranchAnalysis.slnx")
        ];

        foreach (string solutionFile in solutionFiles)
        {
            string content = File.ReadAllText(Path.Combine(repositoryRoot, solutionFile));

            content.Should().NotContain("<Folder Name=\"/docs/\"");
        }
    }

    [Fact]
    public void GitignoreShouldProtectLocalArtifactsAndArchives()
    {
        string content = ReadRepositoryFile(".gitignore");

        content.Should().Contain(".vs/");
        content.Should().Contain("*.user");
        content.Should().Contain("bin/");
        content.Should().Contain("obj/");
        content.Should().Contain("TestResults/");
        content.Should().Contain("artifacts/");
        content.Should().Contain("*.zip");
        content.Should().Contain("*.7z");
        content.Should().Contain("*.rar");
        content.Should().Contain("older-files/");
        content.Should().Contain("docs/");
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
