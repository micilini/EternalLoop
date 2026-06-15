using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.IO;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.IO;

public sealed class BranchAnalysisWriterTests
{
    [Fact]
    public void BranchWriterShouldWriteDefaultFileName()
    {
        string outputRoot = CreateTempDirectory();

        BranchAnalysisWriteResult result = BranchAnalysisWriter.Write(outputRoot, "song-a", CreatePayload());

        Path.GetFileName(result.OutputPath).Should().Be(BranchAnalysisWriter.BranchFileName);
        File.Exists(result.OutputPath).Should().BeTrue();
        JsonNode node = JsonNode.Parse(File.ReadAllText(result.OutputPath))!;
        node["schemaVersion"]!.GetValue<string>().Should().Be("eternalloop-branch-export-v1");
    }

    [Fact]
    public void BranchWriterShouldCreateOutputDirectory()
    {
        string outputRoot = CreateTempDirectory();

        BranchAnalysisWriteResult result = BranchAnalysisWriter.Write(outputRoot, "nested-song", CreatePayload());

        Directory.Exists(result.OutputDirectory).Should().BeTrue();
    }

    [Fact]
    public void BranchWriterShouldRefuseOverwriteWithoutForce()
    {
        string outputRoot = CreateTempDirectory();
        BranchAnalysisWriter.Write(outputRoot, "song-a", CreatePayload());

        Action act = () => BranchAnalysisWriter.Write(outputRoot, "song-a", CreatePayload());

        act.Should().Throw<BranchOutputAlreadyExistsException>()
            .WithMessage("Branch output already exists: *");
    }

    [Fact]
    public void BranchWriterShouldOverwriteWithForce()
    {
        string outputRoot = CreateTempDirectory();
        BranchAnalysisWriteResult first = BranchAnalysisWriter.Write(outputRoot, "song-a", CreatePayload("first"));

        BranchAnalysisWriteResult second = BranchAnalysisWriter.Write(
            outputRoot,
            "song-a",
            CreatePayload("second"),
            new BranchAnalysisWriteOptions { Force = true });

        second.OutputPath.Should().Be(first.OutputPath);
        string content = File.ReadAllText(second.OutputPath);
        content.Should().Contain("second");
        content.Should().NotContain("first");
    }

    [Fact]
    public void BranchWriterShouldWriteCompactJson()
    {
        string outputRoot = CreateTempDirectory();

        BranchAnalysisWriteResult result = BranchAnalysisWriter.Write(
            outputRoot,
            "song-a",
            CreatePayload(),
            new BranchAnalysisWriteOptions { Pretty = false });

        string content = File.ReadAllText(result.OutputPath);
        content.Should().NotContain("\n");
        content.Should().NotContain("\r");
    }

    [Fact]
    public void BranchWriterShouldWritePrettyJsonByDefault()
    {
        string outputRoot = CreateTempDirectory();

        BranchAnalysisWriteResult result = BranchAnalysisWriter.Write(outputRoot, "song-a", CreatePayload());

        string content = File.ReadAllText(result.OutputPath);
        content.Should().Contain(Environment.NewLine);
        content.Should().Contain($"{Environment.NewLine}  \"schemaVersion\"");
    }

    [Fact]
    public void WriteShouldWriteUtf8WithoutBom()
    {
        string outputRoot = CreateTempDirectory();

        BranchAnalysisWriteResult result = BranchAnalysisWriter.Write(outputRoot, "song-a", CreatePayload());

        byte[] bytes = File.ReadAllBytes(result.OutputPath);
        bytes.Should().NotBeEmpty();
        (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF).Should().BeFalse();
        bytes[0].Should().Be((byte)'{');
        File.ReadAllText(result.OutputPath).Should().NotStartWith("\uFEFF");
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateTempSubdirectory("eternalloop-branch-writer-").FullName;
    }

    private static JsonNode CreatePayload(string id = "song-a")
    {
        return new JsonObject
        {
            ["schemaVersion"] = "eternalloop-branch-export-v1",
            ["track"] = new JsonObject
            {
                ["id"] = id
            },
            ["activeBranches"] = new JsonArray(),
            ["candidateBranches"] = new JsonArray()
        };
    }
}
