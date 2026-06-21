using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class OnnxBeatModelRuntimeTests
{
    [Fact]
    public void Constructor_throws_when_model_path_is_empty()
    {
        var act = () => new OnnxBeatModelRuntime("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_throws_clear_error_when_model_file_is_missing()
    {
        var modelPath = Path.Combine(
            Path.GetTempPath(),
            "eternalloop-missing-models",
            Guid.NewGuid().ToString("N"),
            "beat-this-large.onnx");

        var act = () => new OnnxBeatModelRuntime(modelPath);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("ONNX beat model file not found:*");
    }

    [Fact]
    public void Runtime_implements_inference_contract()
    {
        typeof(IBeatModelRuntime)
            .GetMethod(nameof(IBeatModelRuntime.Run))
            .Should()
            .NotBeNull();
    }
}