using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Tools;

public sealed class ImportBeatThisModelScriptTests
{
    private const string ScriptRelativePath = "tools/import-beat-this-model.ps1";

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

        content.Should().Contain("$SourceModelPath");
        content.Should().Contain("$ModelDirectory");
        content.Should().Contain("$ModelFileName");
        content.Should().Contain("$License");
        content.Should().Contain("$InputName");
        content.Should().Contain("$OutputNames");
        content.Should().Contain("$Force");
    }

    [Fact]
    public async Task Script_uses_expected_default_asset_location_and_file_names()
    {
        var content = await ReadScriptAsync();

        content.Should().Contain("assets/models/beat-this");
        content.Should().Contain("beat-this-large.onnx");
        content.Should().Contain("model.json");
    }

    [Fact]
    public async Task Script_calculates_sha256_and_writes_metadata_json()
    {
        var content = await ReadScriptAsync();

        content.Should().Contain("Get-FileHash");
        content.Should().Contain("SHA256");
        content.Should().Contain("ConvertTo-Json");
        content.Should().Contain("model_sha256");
    }

    [Fact]
    public async Task Script_writes_preprocessor_contract_metadata_fields()
    {
        var content = await ReadScriptAsync();

        content.Should().Contain("$ChunkFrames");
        content.Should().Contain("$MelBins");
        content.Should().Contain("$FrameSize");
        content.Should().Contain("onnx_kind");
        content.Should().Contain("chunk_frames");
        content.Should().Contain("mel_bins");
        content.Should().Contain("frame_size");
    }

    [Fact]
    public async Task Script_rejects_non_onnx_sources()
    {
        var content = await ReadScriptAsync();

        content.Should().Contain("Source model must be an .onnx file");
        content.Should().Contain("ModelFileName must end with .onnx");
    }

    [Fact]
    public async Task Script_does_not_download_or_reference_product_ui_projects()
    {
        var content = await ReadScriptAsync();

        content.Should().NotContain("Invoke-WebRequest");
        content.Should().NotContain("curl");
        content.Should().NotContain("EternalLoop.App");
        content.Should().NotContain("EternalLoop.Playback");
        content.Should().NotContain("BranchAnalysis");
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