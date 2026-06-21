namespace EternalLoop.Core.Settings;

public static class AnalysisBeatModeCatalog
{
    public const string EnhancedId = "Enhanced";

    public const string ClassicId = "Classic";

    private static readonly AnalysisBeatModeDefinition Enhanced = new(
        EnhancedId,
        "Enhanced Analysis",
        "Uses local AI timing analysis to understand rhythm, beats, and jump timing.");

    private static readonly AnalysisBeatModeDefinition Classic = new(
        ClassicId,
        "Classic Analysis",
        "Uses EternalLoop's original deterministic timing analysis.");

    public static IReadOnlyList<AnalysisBeatModeDefinition> All { get; } =
    [
        Enhanced,
        Classic
    ];

    public static AnalysisBeatModeDefinition GetById(string? id)
    {
        return All.FirstOrDefault(option =>
                string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? Enhanced;
    }

    public static bool IsEnhanced(string? id)
    {
        return string.Equals(
            GetById(id).Id,
            EnhancedId,
            StringComparison.Ordinal);
    }
}
