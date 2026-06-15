using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Tools;

public sealed class CopyToLoopLabScriptTests
{
    private const string ScriptRelativePath = "tools/copy-to-loop-lab.ps1";

    [Fact]
    public void Script_exists_in_tools_directory()
    {
        var scriptPath = GetScriptPath();

        File.Exists(scriptPath).Should().BeTrue();
    }

    [Fact]
    public async Task Script_contains_required_parameters()
    {
        var content = await ReadScriptAsync();

        content.Should().Contain("$ExporterOutputDir");
        content.Should().Contain("$LoopLabRoot");
        content.Should().Contain("$TrackId");
        content.Should().Contain("$CopyRaw");
    }

    [Fact]
    public async Task Script_uses_expected_export_file_names()
    {
        var content = await ReadScriptAsync();

        content.Should().Contain("eternalloop-analysis.json");
        content.Should().Contain("analysis");
        content.Should().Contain("reference");
    }

    [Fact]
    public async Task Script_prints_expected_lab_urls()
    {
        var content = await ReadScriptAsync();

        content.Should().Contain("loop-map.html?id=");
        content.Should().Contain("loop-side-by-side.html");
    }

    [Fact]
    public async Task Script_does_not_reference_frozen_eternalloop_namespaces()
    {
        var content = await ReadScriptAsync();

        content.Should().NotContain("EternalLoop.Contracts");
        content.Should().NotContain("EternalLoop.Core");
        content.Should().NotContain("EternalLoop.App");
    }

    private static async Task<string> ReadScriptAsync()
    {
        return await File.ReadAllTextAsync(GetScriptPath());
    }

    private static string GetScriptPath()
    {
        return Path.Combine(GetRepositoryRoot(), ScriptRelativePath);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "EternalLoop.AnalysisEngine.slnx");

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find EternalLoop.AnalysisEngine.slnx.");
    }
}
