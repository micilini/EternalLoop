using System.Text.Json;
using System.Text.Json.Serialization;

namespace EternalLoop.AnalysisEngine.Core.Export;

public static class JsonWriterOptionsFactory
{
    public static JsonSerializerOptions Create(bool pretty)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = pretty,
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
    }
}
