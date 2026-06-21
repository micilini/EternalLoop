using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Tools;

public sealed class BeatThisConversionToolingTests
{
    [Fact]
    public void Conversion_tooling_files_exist()
    {
        var root = GetAnalysisEngineRoot();
        var toolRoot = Path.Combine(root, "tools", "beat-this-conversion");

        File.Exists(Path.Combine(toolRoot, "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(toolRoot, "requirements.txt")).Should().BeTrue();
        File.Exists(Path.Combine(toolRoot, "sample_input_contract.json")).Should().BeTrue();
        File.Exists(Path.Combine(toolRoot, "export_beat_this_to_onnx.py")).Should().BeTrue();
        File.Exists(Path.Combine(toolRoot, "verify_onnx.py")).Should().BeTrue();
    }

    [Fact]
    public async Task Export_script_uses_torch_onnx_export_and_expected_wrapper()
    {
        var script = await ReadToolFileAsync("export_beat_this_to_onnx.py");

        script.Should().Contain("torch.onnx.export");
        script.Should().Contain("BeatThisOnnxWrapper");
        script.Should().Contain("from beat_this.inference import load_model");
        script.Should().Contain("beat_logits");
        script.Should().Contain("downbeat_logits");
    }

    [Fact]
    public async Task Verify_script_uses_onnxruntime_cpu_execution_provider()
    {
        var script = await ReadToolFileAsync("verify_onnx.py");

        script.Should().Contain("onnxruntime");
        script.Should().Contain("InferenceSession");
        script.Should().Contain("CPUExecutionProvider");
    }

    [Fact]
    public async Task Input_contract_documents_spectrogram_chunk_shape_and_outputs()
    {
        var contract = await ReadToolFileAsync("sample_input_contract.json");

        contract.Should().Contain("spectrogram");
        contract.Should().Contain("beat_logits");
        contract.Should().Contain("downbeat_logits");
        contract.Should().Contain("chunk_frames");
        contract.Should().Contain("border_frames");
        contract.Should().Contain("mel_bins");
    }

    [Fact]
    public async Task Requirements_file_keeps_python_dependencies_in_tooling_only()
    {
        var requirements = await ReadToolFileAsync("requirements.txt");

        requirements.Should().Contain("beat-this");
        requirements.Should().Contain("torch");
        requirements.Should().Contain("onnxruntime");
    }

    [Fact]
    public async Task Conversion_tooling_does_not_reference_product_ui_projects()
    {
        var exportScript = await ReadToolFileAsync("export_beat_this_to_onnx.py");
        var verifyScript = await ReadToolFileAsync("verify_onnx.py");
        var combined = exportScript + "\n" + verifyScript;

        combined.Should().NotContain("EternalLoop.App");
        combined.Should().NotContain("BranchAnalysis");
        combined.Should().NotContain("WPF");
        combined.Should().NotContain("PlayerView");
    }

    private static async Task<string> ReadToolFileAsync(string fileName)
    {
        var root = GetAnalysisEngineRoot();
        var path = Path.Combine(root, "tools", "beat-this-conversion", fileName);

        return await File.ReadAllTextAsync(path);
    }

    private static string GetAnalysisEngineRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "modules", "AnalysisEngine", "EternalLoop.AnalysisEngine.slnx");

            if (File.Exists(candidate))
            {
                return Path.Combine(directory.FullName, "modules", "AnalysisEngine");
            }

            var directCandidate = Path.Combine(directory.FullName, "EternalLoop.AnalysisEngine.slnx");

            if (File.Exists(directCandidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate modules/AnalysisEngine root.");
    }
}