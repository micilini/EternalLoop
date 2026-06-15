using System.Globalization;
using System.Text;

namespace EternalLoop.AnalysisEngine.Cli;

public static class TrackIdNormalizer
{
    private const char Separator = '-';
    private const string FallbackTrackId = "local-audio";

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FallbackTrackId;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var pendingSeparator = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append(Separator);
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingSeparator = false;
                continue;
            }

            if (character == Separator || character == '_' || character == '.' || char.IsWhiteSpace(character))
            {
                pendingSeparator = builder.Length > 0;
            }
        }

        var result = builder.ToString().Trim(Separator);

        return string.IsNullOrWhiteSpace(result) ? FallbackTrackId : result;
    }
}
