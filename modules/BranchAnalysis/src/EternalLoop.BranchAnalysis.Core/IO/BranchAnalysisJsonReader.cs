using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EternalLoop.BranchAnalysis.Core.IO;

public static class BranchAnalysisJsonReader
{
    public static JsonNode Read(string filePath)
    {
        string content;

        try
        {
            content = File.ReadAllText(filePath, Encoding.UTF8);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new BranchAnalysisJsonReadException(filePath, exception);
        }

        try
        {
            JsonNode? node = JsonNode.Parse(content);

            if (node is null)
            {
                throw new JsonException("JSON root cannot be null.");
            }

            return node;
        }
        catch (JsonException exception)
        {
            throw new BranchAnalysisJsonParseException(filePath, exception);
        }
    }
}
