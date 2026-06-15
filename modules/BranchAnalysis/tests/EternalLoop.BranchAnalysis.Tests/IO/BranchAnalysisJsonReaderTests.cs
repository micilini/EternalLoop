using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.IO;
using FluentAssertions;

namespace EternalLoop.BranchAnalysis.Tests.IO;

public sealed class BranchAnalysisJsonReaderTests
{
    [Fact]
    public void JsonReaderShouldReadValidJson()
    {
        string filePath = CreateTempFile("""{"ok":true}""");

        JsonNode node = BranchAnalysisJsonReader.Read(filePath);

        node["ok"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void JsonReaderShouldThrowForMissingFile()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"missing-json-{Guid.NewGuid():N}.json");

        Action act = () => BranchAnalysisJsonReader.Read(filePath);

        act.Should().Throw<BranchAnalysisJsonReadException>()
            .WithMessage($"Failed to read JSON file: {filePath}. *");
    }

    [Fact]
    public void JsonReaderShouldThrowForInvalidJson()
    {
        string filePath = CreateTempFile("{invalid-json");

        Action act = () => BranchAnalysisJsonReader.Read(filePath);

        act.Should().Throw<BranchAnalysisJsonParseException>()
            .WithMessage($"Failed to parse JSON file: {filePath}. *");
    }

    [Fact]
    public void JsonReaderShouldThrowForNullJsonRoot()
    {
        string filePath = CreateTempFile("null");

        Action act = () => BranchAnalysisJsonReader.Read(filePath);

        act.Should().Throw<BranchAnalysisJsonParseException>()
            .WithMessage($"Failed to parse JSON file: {filePath}. JSON root cannot be null.");
    }

    private static string CreateTempFile(string content)
    {
        string directory = Directory.CreateTempSubdirectory("eternalloop-branch-json-reader-").FullName;
        string filePath = Path.Combine(directory, "analysis.json");
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
