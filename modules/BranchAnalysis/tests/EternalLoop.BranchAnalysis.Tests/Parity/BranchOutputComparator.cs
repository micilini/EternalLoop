using System.Text.Json.Nodes;

namespace EternalLoop.BranchAnalysis.Tests.Parity;

public static class BranchOutputComparator
{
    public static BranchOutputComparisonResult Compare(
        JsonNode nodeOutput,
        JsonNode csharpOutput,
        BranchOutputComparisonOptions? options = null)
    {
        BranchOutputComparisonOptions effectiveOptions = options ?? new BranchOutputComparisonOptions();
        JsonNode normalizedNode = BranchOutputNormalizer.Normalize(nodeOutput, effectiveOptions);
        JsonNode normalizedCSharp = BranchOutputNormalizer.Normalize(csharpOutput, effectiveOptions);
        List<BranchOutputDifference> differences = [];

        CompareNodes(normalizedNode, normalizedCSharp, "$", effectiveOptions, differences);

        return new BranchOutputComparisonResult
        {
            Differences = differences
        };
    }

    private static void CompareNodes(
        JsonNode? node,
        JsonNode? csharp,
        string path,
        BranchOutputComparisonOptions options,
        List<BranchOutputDifference> differences)
    {
        if (node is null || csharp is null)
        {
            if (node is not null || csharp is not null)
            {
                AddDifference(differences, path, node, csharp);
            }

            return;
        }

        if (node is JsonObject nodeObject && csharp is JsonObject csharpObject)
        {
            CompareObjects(nodeObject, csharpObject, path, options, differences);
            return;
        }

        if (node is JsonArray nodeArray && csharp is JsonArray csharpArray)
        {
            CompareArrays(nodeArray, csharpArray, path, options, differences);
            return;
        }

        if (node is JsonValue nodeValue && csharp is JsonValue csharpValue)
        {
            CompareValues(nodeValue, csharpValue, path, options, differences);
            return;
        }

        AddDifference(differences, path, node, csharp);
    }

    private static void CompareObjects(
        JsonObject node,
        JsonObject csharp,
        string path,
        BranchOutputComparisonOptions options,
        List<BranchOutputDifference> differences)
    {
        SortedSet<string> keys = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, JsonNode?> property in node)
        {
            keys.Add(property.Key);
        }

        foreach (KeyValuePair<string, JsonNode?> property in csharp)
        {
            keys.Add(property.Key);
        }

        foreach (string key in keys)
        {
            bool nodeHasKey = node.ContainsKey(key);
            bool csharpHasKey = csharp.ContainsKey(key);
            string childPath = path == "$" ? key : $"{path}.{key}";

            if (!nodeHasKey || !csharpHasKey)
            {
                AddDifference(differences, childPath, nodeHasKey ? node[key] : null, csharpHasKey ? csharp[key] : null);
                continue;
            }

            CompareNodes(node[key], csharp[key], childPath, options, differences);
        }
    }

    private static void CompareArrays(
        JsonArray node,
        JsonArray csharp,
        string path,
        BranchOutputComparisonOptions options,
        List<BranchOutputDifference> differences)
    {
        if (node.Count != csharp.Count)
        {
            differences.Add(new BranchOutputDifference
            {
                Path = $"{path}.Count",
                NodeValue = node.Count.ToString(),
                CSharpValue = csharp.Count.ToString()
            });
            return;
        }

        for (int index = 0; index < node.Count; index++)
        {
            CompareNodes(node[index], csharp[index], $"{path}[{index}]", options, differences);
        }
    }

    private static void CompareValues(
        JsonValue node,
        JsonValue csharp,
        string path,
        BranchOutputComparisonOptions options,
        List<BranchOutputDifference> differences)
    {
        if (node.TryGetValue<double>(out double nodeNumber) && csharp.TryGetValue<double>(out double csharpNumber))
        {
            if (double.IsFinite(nodeNumber)
                && double.IsFinite(csharpNumber)
                && Math.Abs(nodeNumber - csharpNumber) <= options.NumericTolerance)
            {
                return;
            }
        }

        string nodeText = ValueToString(node);
        string csharpText = ValueToString(csharp);

        if (!string.Equals(nodeText, csharpText, StringComparison.Ordinal))
        {
            differences.Add(new BranchOutputDifference
            {
                Path = path,
                NodeValue = nodeText,
                CSharpValue = csharpText
            });
        }
    }

    private static void AddDifference(List<BranchOutputDifference> differences, string path, JsonNode? node, JsonNode? csharp)
    {
        differences.Add(new BranchOutputDifference
        {
            Path = path,
            NodeValue = NodeToString(node),
            CSharpValue = NodeToString(csharp)
        });
    }

    private static string NodeToString(JsonNode? node)
    {
        return node?.ToJsonString() ?? "null";
    }

    private static string ValueToString(JsonValue value)
    {
        return value.TryGetValue<string>(out string? stringValue) ? stringValue ?? "null" : value.ToJsonString();
    }
}
