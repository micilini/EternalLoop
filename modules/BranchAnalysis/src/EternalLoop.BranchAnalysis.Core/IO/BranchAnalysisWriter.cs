using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.Export;

namespace EternalLoop.BranchAnalysis.Core.IO;

public static class BranchAnalysisWriter
{
    public const string BranchFileName = "eternalloop-branches.json";
    public const int JsonIndentSpaces = 2;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static BranchAnalysisWriteResult Write(
        string outputRoot,
        string analysisName,
        JsonNode payload,
        BranchAnalysisWriteOptions? options = null)
    {
        BranchAnalysisWriteOptions effectiveOptions = options ?? new BranchAnalysisWriteOptions();
        string outputDirectory = Path.GetFullPath(Path.Combine(outputRoot, analysisName));
        string outputPath = Path.Combine(outputDirectory, BranchFileName);

        Directory.CreateDirectory(outputDirectory);

        if (File.Exists(outputPath) && !effectiveOptions.Force)
        {
            throw new BranchOutputAlreadyExistsException(outputPath);
        }

        JsonSerializerOptions serializerOptions = new()
        {
            WriteIndented = effectiveOptions.Pretty
        };
        string json = payload.ToJsonString(serializerOptions);

        File.WriteAllText(outputPath, json, Utf8NoBom);

        return new BranchAnalysisWriteResult
        {
            OutputDirectory = outputDirectory,
            OutputPath = outputPath
        };
    }

    public static BranchAnalysisWriteResult Write(
        string outputRoot,
        string analysisName,
        BranchExportPayload payload,
        BranchAnalysisWriteOptions? options = null)
    {
        return Write(outputRoot, analysisName, BranchExportPayloadBuilder.ToJsonNode(payload), options);
    }
}
