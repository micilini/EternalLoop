using System.Text.Json;
using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Validation;

public static class AnalysisContractValidator
{
    private static readonly string[] RootObjects = ["info", "analysis", "audio_summary"];
    private static readonly string[] AnalysisArrays = ["sections", "bars", "beats", "tatums", "segments"];
    private static readonly string[] TimeQuantumFields = ["start", "duration", "confidence"];
    private static readonly string[] SegmentNumberFields =
    [
        "start",
        "duration",
        "confidence",
        "loudness_start",
        "loudness_max",
        "loudness_max_time"
    ];

    public static TrackAnalysisDocument ReadValidated(JsonNode? root)
    {
        Validate(root);

        TrackAnalysisDocument? document = root!.Deserialize<TrackAnalysisDocument>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = false });

        if (document is null)
        {
            throw new AnalysisContractValidationException("Analysis root could not be deserialized.");
        }

        return document;
    }

    public static void Validate(JsonNode? root)
    {
        JsonObject rootObject = RequirePlainObject(root, "Analysis root");

        foreach (string rootField in RootObjects)
        {
            RequireObject(rootObject, rootField, rootField);
        }

        JsonObject audioSummary = RequireObject(rootObject, "audio_summary", "audio_summary");
        RequireFiniteNumber(audioSummary, "duration", "audio_summary.duration");

        JsonObject analysis = RequireObject(rootObject, "analysis", "analysis");

        foreach (string arrayName in AnalysisArrays)
        {
            RequireArray(analysis, arrayName, $"analysis.{arrayName}");
        }

        ValidateTimeQuanta(RequireArray(analysis, "sections", "analysis.sections"), "analysis.sections");
        ValidateTimeQuanta(RequireArray(analysis, "bars", "analysis.bars"), "analysis.bars");
        ValidateTimeQuanta(RequireArray(analysis, "beats", "analysis.beats"), "analysis.beats");
        ValidateTimeQuanta(RequireArray(analysis, "tatums", "analysis.tatums"), "analysis.tatums");
        ValidateSegments(RequireArray(analysis, "segments", "analysis.segments"));
    }

    private static JsonObject RequireObject(JsonObject source, string field, string path)
    {
        if (!source.TryGetPropertyValue(field, out JsonNode? node))
        {
            throw new AnalysisContractValidationException($"{path} must be an object.");
        }

        return RequirePlainObject(node, path);
    }

    private static JsonObject RequirePlainObject(JsonNode? node, string path)
    {
        if (node is JsonObject jsonObject)
        {
            return jsonObject;
        }

        throw new AnalysisContractValidationException($"{path} must be an object.");
    }

    private static JsonArray RequireArray(JsonObject source, string field, string path)
    {
        if (!source.TryGetPropertyValue(field, out JsonNode? node) || node is not JsonArray array)
        {
            throw new AnalysisContractValidationException($"{path} must be an array.");
        }

        return array;
    }

    private static void RequireFiniteNumber(JsonObject source, string field, string path)
    {
        if (!source.TryGetPropertyValue(field, out JsonNode? node) || !IsFiniteNumber(node))
        {
            throw new AnalysisContractValidationException($"{path} must be a finite number.");
        }
    }

    private static void RequireNumericArray(JsonObject source, string field, string path)
    {
        if (!source.TryGetPropertyValue(field, out JsonNode? node) || node is not JsonArray array)
        {
            throw new AnalysisContractValidationException($"{path} must be an array.");
        }

        for (int index = 0; index < array.Count; index++)
        {
            if (!IsFiniteNumber(array[index]))
            {
                throw new AnalysisContractValidationException($"{path}[{index}] must be a finite number.");
            }
        }
    }

    private static void ValidateTimeQuanta(JsonArray quanta, string path)
    {
        for (int index = 0; index < quanta.Count; index++)
        {
            string quantumPath = $"{path}[{index}]";
            JsonObject quantum = RequirePlainObject(quanta[index], quantumPath);

            foreach (string field in TimeQuantumFields)
            {
                RequireFiniteNumber(quantum, field, $"{quantumPath}.{field}");
            }
        }
    }

    private static void ValidateSegments(JsonArray segments)
    {
        for (int index = 0; index < segments.Count; index++)
        {
            string segmentPath = $"analysis.segments[{index}]";
            JsonObject segment = RequirePlainObject(segments[index], segmentPath);

            foreach (string field in SegmentNumberFields)
            {
                RequireFiniteNumber(segment, field, $"{segmentPath}.{field}");
            }

            RequireNumericArray(segment, "pitches", $"{segmentPath}.pitches");
            RequireNumericArray(segment, "timbre", $"{segmentPath}.timbre");
        }
    }

    private static bool IsFiniteNumber(JsonNode? node)
    {
        if (node is not JsonValue value)
        {
            return false;
        }

        if (value.TryGetValue<double>(out double doubleValue))
        {
            return double.IsFinite(doubleValue);
        }

        if (value.TryGetValue<float>(out float floatValue))
        {
            return float.IsFinite(floatValue);
        }

        return value.TryGetValue<decimal>(out _)
            || value.TryGetValue<long>(out _)
            || value.TryGetValue<int>(out _);
    }
}
