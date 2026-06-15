using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.Models;
using EternalLoop.BranchAnalysis.Core.Validation;
using EternalLoop.BranchAnalysis.Tests.Fixtures;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.Validation;

public sealed class AnalysisContractValidatorTests
{
    [Fact]
    public void ValidatorShouldAcceptValidAnalysis()
    {
        JsonNode node = AnalysisFixtureFactory.CreateValidAnalysisNode();

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatorShouldRejectNullRoot()
    {
        Action act = () => AnalysisContractValidator.Validate(null);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("Analysis root must be an object.");
    }

    [Fact]
    public void ValidatorShouldRejectArrayRoot()
    {
        JsonNode node = new JsonArray();

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("Analysis root must be an object.");
    }

    [Fact]
    public void ValidatorShouldRejectMissingInfoRoot()
    {
        JsonObject node = CreateValidObject();
        node.Remove("info");

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("info must be an object.");
    }

    [Fact]
    public void ValidatorShouldRejectMissingAnalysisRoot()
    {
        JsonObject node = CreateValidObject();
        node.Remove("analysis");

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("analysis must be an object.");
    }

    [Fact]
    public void ValidatorShouldRejectMissingAudioSummaryRoot()
    {
        JsonObject node = CreateValidObject();
        node.Remove("audio_summary");

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("audio_summary must be an object.");
    }

    [Fact]
    public void ValidatorShouldRejectMissingAudioSummaryDuration()
    {
        JsonObject node = CreateValidObject();
        AudioSummary(node).Remove("duration");

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("audio_summary.duration must be a finite number.");
    }

    [Fact]
    public void ValidatorShouldRejectInvalidAudioSummaryDuration()
    {
        JsonObject node = CreateValidObject();
        AudioSummary(node)["duration"] = "bad";

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("audio_summary.duration must be a finite number.");
    }

    [Theory]
    [InlineData("sections", "analysis.sections must be an array.")]
    [InlineData("bars", "analysis.bars must be an array.")]
    [InlineData("beats", "analysis.beats must be an array.")]
    [InlineData("tatums", "analysis.tatums must be an array.")]
    [InlineData("segments", "analysis.segments must be an array.")]
    public void ValidatorShouldRejectMissingAnalysisArrays(string arrayName, string expectedMessage)
    {
        JsonObject node = CreateValidObject();
        Analysis(node).Remove(arrayName);

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void ValidatorShouldRejectQuantumWithoutStart()
    {
        JsonObject node = CreateValidObject();
        ((JsonObject)AnalysisArray(node, "beats")[0]!).Remove("start");

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("analysis.beats[0].start must be a finite number.");
    }

    [Fact]
    public void ValidatorShouldRejectQuantumWithNonNumericDuration()
    {
        JsonObject node = CreateValidObject();
        ((JsonObject)AnalysisArray(node, "beats")[0]!)["duration"] = "bad";

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("analysis.beats[0].duration must be a finite number.");
    }

    [Fact]
    public void ValidatorShouldRejectSegmentWithoutTimbre()
    {
        JsonObject node = CreateValidObject();
        FirstSegment(node).Remove("timbre");

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("analysis.segments[0].timbre must be an array.");
    }

    [Fact]
    public void ValidatorShouldRejectSegmentWithNonNumericPitch()
    {
        JsonObject node = CreateValidObject();
        ((JsonArray)FirstSegment(node)["pitches"]!)[0] = "bad";

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("analysis.segments[0].pitches[0] must be a finite number.");
    }

    [Fact]
    public void ValidatorShouldRejectSegmentWithNonNumericTimbre()
    {
        JsonObject node = CreateValidObject();
        ((JsonArray)FirstSegment(node)["timbre"]!)[0] = "bad";

        Action act = () => AnalysisContractValidator.Validate(node);

        act.Should().Throw<AnalysisContractValidationException>()
            .WithMessage("analysis.segments[0].timbre[0] must be a finite number.");
    }

    [Fact]
    public void ReadValidatedShouldReturnTypedDocument()
    {
        JsonNode node = AnalysisFixtureFactory.CreateValidAnalysisNode();

        TrackAnalysisDocument document = AnalysisContractValidator.ReadValidated(node);

        document.Info.Id.Should().Be("fixture-track");
        document.AudioSummary.Duration.Should().Be(8);
        document.Analysis.Beats.Should().HaveCount(8);
        document.Analysis.Segments.Should().HaveCount(8);
    }

    private static JsonObject CreateValidObject()
    {
        return (JsonObject)AnalysisFixtureFactory.CreateValidAnalysisNode();
    }

    private static JsonObject Analysis(JsonObject node)
    {
        return (JsonObject)node["analysis"]!;
    }

    private static JsonObject AudioSummary(JsonObject node)
    {
        return (JsonObject)node["audio_summary"]!;
    }

    private static JsonArray AnalysisArray(JsonObject node, string name)
    {
        return (JsonArray)Analysis(node)[name]!;
    }

    private static JsonObject FirstSegment(JsonObject node)
    {
        return (JsonObject)AnalysisArray(node, "segments")[0]!;
    }
}
