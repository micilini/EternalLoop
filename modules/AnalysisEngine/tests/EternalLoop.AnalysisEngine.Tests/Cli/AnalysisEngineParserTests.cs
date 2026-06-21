using EternalLoop.AnalysisEngine.Cli;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Cli;

public sealed class AnalysisEngineParserTests
{
    [Fact]
    public void Parser_accepts_minimal_command()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.InputPath.Should().Be("C:\\Music\\song.wav");
        result.Arguments.OutputDirectory.Should().Be("C:\\Exports");
        result.Arguments.BeatProvider.Should().Be(BeatTrackingProviderKind.Auto);
        result.Arguments.AiFallbackMode.Should().Be(AiFallbackMode.FallbackToBuiltIn);
    }

    [Fact]
    public void Parser_rejects_missing_input()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--output-dir",
            "C:\\Exports"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("--input");
    }

    [Fact]
    public void Parser_rejects_missing_output_dir()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("--output-dir");
    }

    [Fact]
    public void Parser_rejects_invalid_format()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--format",
            "summary"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid format");
    }

    [Theory]
    [InlineData("auto", BeatTrackingProviderKind.Auto)]
    [InlineData("built-in", BeatTrackingProviderKind.BuiltIn)]
    [InlineData("builtin", BeatTrackingProviderKind.BuiltIn)]
    [InlineData("built_in", BeatTrackingProviderKind.BuiltIn)]
    [InlineData("beat-this", BeatTrackingProviderKind.BeatThis)]
    [InlineData("beatthis", BeatTrackingProviderKind.BeatThis)]
    [InlineData("beat_this", BeatTrackingProviderKind.BeatThis)]
    [InlineData("shadow", BeatTrackingProviderKind.Shadow)]
    [InlineData("shadow-mode", BeatTrackingProviderKind.Shadow)]
    [InlineData("beat-this-shadow", BeatTrackingProviderKind.Shadow)]
    [InlineData("built-in-shadow", BeatTrackingProviderKind.Shadow)]
    [InlineData("legacy-shadow", BeatTrackingProviderKind.Shadow)]
    [InlineData("hybrid", BeatTrackingProviderKind.Hybrid)]
    [InlineData("hybrid-experimental", BeatTrackingProviderKind.Hybrid)]
    [InlineData("weak-window-hybrid", BeatTrackingProviderKind.Hybrid)]
    public void Parser_accepts_beat_provider_values(string value, BeatTrackingProviderKind expected)
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--beat-provider",
            value
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.BeatProvider.Should().Be(expected);
    }

    [Fact]
    public void Parser_rejects_invalid_beat_provider()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--beat-provider",
            "banana"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid beat provider. Expected auto, built-in, beat-this, shadow, or hybrid.");
    }

    [Theory]
    [InlineData("fallback-to-built-in", AiFallbackMode.FallbackToBuiltIn)]
    [InlineData("fallback", AiFallbackMode.FallbackToBuiltIn)]
    [InlineData("fallback-to-builtin", AiFallbackMode.FallbackToBuiltIn)]
    [InlineData("fail", AiFallbackMode.Fail)]
    public void Parser_accepts_ai_fallback_values(string value, AiFallbackMode expected)
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--ai-fallback",
            value
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.AiFallbackMode.Should().Be(expected);
    }

    [Fact]
    public void Parser_rejects_invalid_ai_fallback()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--ai-fallback",
            "banana"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid AI fallback mode. Expected fallback-to-built-in or fail.");
    }

    [Fact]
    public void Parser_defaults_tatum_mode_to_default()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.TatumMode.Should().Be(TatumMode.Default);
    }

    [Theory]
    [InlineData("default", TatumMode.Default)]
    [InlineData("adaptive", TatumMode.Adaptive)]
    [InlineData("fixed", TatumMode.FixedTwoPerBeat)]
    [InlineData("fixed-two-per-beat", TatumMode.FixedTwoPerBeat)]
    [InlineData("fixed_two_per_beat", TatumMode.FixedTwoPerBeat)]
    [InlineData("two-per-beat", TatumMode.FixedTwoPerBeat)]
    [InlineData("twoperbeat", TatumMode.FixedTwoPerBeat)]
    public void Parser_accepts_tatum_mode_values(string value, TatumMode expected)
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--tatum-mode",
            value
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.TatumMode.Should().Be(expected);
    }

    [Fact]
    public void Parser_rejects_invalid_tatum_mode()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--tatum-mode",
            "banana"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid tatum mode. Expected default, adaptive, or fixed-two-per-beat.");
    }

    [Fact]
    public void Parser_normalizes_track_id()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\Música Teste - João.mp3",
            "--output-dir",
            "C:\\Exports"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.TrackId.Should().Be("musica-teste-joao");
    }

    [Fact]
    public void Parser_accepts_force()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--force"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.Force.Should().BeTrue();
    }

    [Fact]
    public void Parser_accepts_quiet()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--quiet"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.Quiet.Should().BeTrue();
    }

    [Fact]
    public void Parser_accepts_individual_musical_quality_flags()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--mq-segmentation",
            "--mq-beat-microsnap"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.MusicalQualitySegmentation.Should().BeTrue();
        result.Arguments.MusicalQualityBeatMicroSnap.Should().BeTrue();
        result.Arguments.MusicalQualityTatums.Should().BeFalse();
        result.Arguments.MusicalQualitySections.Should().BeFalse();
        result.Arguments.MusicalQualityConfidences.Should().BeFalse();
    }

    [Fact]
    public void Parser_musical_quality_enables_all_fronts()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--musical-quality"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.MusicalQualitySegmentation.Should().BeTrue();
        result.Arguments.MusicalQualityBeatMicroSnap.Should().BeTrue();
        result.Arguments.MusicalQualityTatums.Should().BeTrue();
        result.Arguments.MusicalQualitySections.Should().BeTrue();
        result.Arguments.MusicalQualityConfidences.Should().BeTrue();
    }

    [Fact]
    public void Parser_accepts_equals_syntax()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input=C:\\Music\\song.wav",
            "--output-dir=C:\\Exports",
            "--format=raw",
            "--track-id=My Song"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.Format.Should().Be(AnalysisEngineFormat.Raw);
        result.Arguments.TrackId.Should().Be("my-song");
    }

    [Fact]
    public void Parser_uses_default_title_artist_format_and_pretty()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\Gangnam Style - PSY.mp3",
            "--output-dir",
            "C:\\Exports"
        ]);

        result.IsSuccess.Should().BeTrue();
        result.Arguments!.Title.Should().Be("Gangnam Style - PSY");
        result.Arguments.Artist.Should().Be(AnalysisOptions.DefaultArtist);
        result.Arguments.Format.Should().Be(AnalysisEngineFormat.Both);
        result.Arguments.Pretty.Should().BeTrue();
        result.Arguments.TrackId.Should().Be("gangnam-style-psy");
    }

    [Fact]
    public void Parser_rejects_boolean_flag_with_value()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--force=true"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not accept a value");
    }

    [Fact]
    public void Parser_rejects_unknown_flag()
    {
        var result = AnalysisEngineParser.Parse(
        [
            "--input",
            "C:\\Music\\song.wav",
            "--output-dir",
            "C:\\Exports",
            "--summary"
        ]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown option");
    }
}
