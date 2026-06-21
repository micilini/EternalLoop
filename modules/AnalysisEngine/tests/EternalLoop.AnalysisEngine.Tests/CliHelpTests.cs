using EternalLoop.AnalysisEngine.Cli;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests;

public sealed class CliHelpTests
{
    [Fact]
    public void Help_contains_input_flag()
    {
        var help = AnalysisEngineHelpWriter.GetHelpText();

        help.Should().Contain("--input <path>");
    }

    [Fact]
    public void Help_contains_output_dir_flag()
    {
        var help = AnalysisEngineHelpWriter.GetHelpText();

        help.Should().Contain("--output-dir <path>");
    }

    [Fact]
    public void Help_contains_beat_provider_flags()
    {
        var help = AnalysisEngineHelpWriter.GetHelpText();

        help.Should().Contain("--beat-provider <mode>");
        help.Should().Contain("--ai-fallback <mode>");
    }
}
