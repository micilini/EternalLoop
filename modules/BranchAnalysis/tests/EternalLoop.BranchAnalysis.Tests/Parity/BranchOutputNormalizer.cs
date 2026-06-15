using System.Text.Json.Nodes;

namespace EternalLoop.BranchAnalysis.Tests.Parity;

public static class BranchOutputNormalizer
{
    private const string StableExportedAt = "<normalized-exportedAt>";
    private const string StableSourcePage = "<normalized-sourcePage>";

    public static JsonNode Normalize(JsonNode node, BranchOutputComparisonOptions? options = null)
    {
        BranchOutputComparisonOptions effectiveOptions = options ?? new BranchOutputComparisonOptions();
        JsonNode clone = node.DeepClone();
        NormalizeNode(clone, effectiveOptions);

        if (clone is JsonObject root)
        {
            root["exportedAt"] = StableExportedAt;
            root["sourcePage"] = StableSourcePage;
            SortBranchArray(root, "activeBranches");
            SortBranchArray(root, "candidateBranches");
        }

        return clone;
    }

    private static void NormalizeNode(JsonNode? node, BranchOutputComparisonOptions options)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (string key in jsonObject.Select(property => property.Key).ToArray())
                {
                    JsonNode? child = jsonObject[key];

                    if (child is JsonValue value && TryGetFiniteDouble(value, out double number))
                    {
                        jsonObject[key] = Math.Round(number, options.DecimalPlaces, MidpointRounding.AwayFromZero);
                        continue;
                    }

                    NormalizeNode(child, options);
                }

                break;

            case JsonArray jsonArray:
                for (int index = 0; index < jsonArray.Count; index++)
                {
                    JsonNode? child = jsonArray[index];

                    if (child is JsonValue value && TryGetFiniteDouble(value, out double number))
                    {
                        jsonArray[index] = Math.Round(number, options.DecimalPlaces, MidpointRounding.AwayFromZero);
                        continue;
                    }

                    NormalizeNode(child, options);
                }

                break;
        }
    }

    private static void SortBranchArray(JsonObject root, string propertyName)
    {
        if (root[propertyName] is not JsonArray branches)
        {
            return;
        }

        List<JsonNode?> sorted = branches
            .Select(branch => branch?.DeepClone())
            .OrderBy(branch => GetNullableInt(branch, "fromBeat") ?? int.MaxValue)
            .ThenBy(branch => GetNullableInt(branch, "toBeat") ?? int.MaxValue)
            .ThenBy(branch => GetNullableDouble(branch, "distance") ?? double.MaxValue)
            .ToList();

        branches.Clear();

        foreach (JsonNode? branch in sorted)
        {
            branches.Add(branch);
        }
    }

    private static int? GetNullableInt(JsonNode? node, string propertyName)
    {
        if (node?[propertyName] is JsonValue value && value.TryGetValue<int>(out int result))
        {
            return result;
        }

        return null;
    }

    private static double? GetNullableDouble(JsonNode? node, string propertyName)
    {
        if (node?[propertyName] is JsonValue value && value.TryGetValue<double>(out double result) && double.IsFinite(result))
        {
            return result;
        }

        return null;
    }

    private static bool TryGetFiniteDouble(JsonValue value, out double number)
    {
        if (value.TryGetValue<double>(out number))
        {
            return double.IsFinite(number);
        }

        return false;
    }
}
