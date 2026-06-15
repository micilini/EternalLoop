namespace EternalLoop.AnalysisEngine.Core.Validation;

public sealed class AnalysisValidationResult
{
    public required IReadOnlyList<string> Errors { get; init; }

    public bool IsValid => Errors.Count == 0;

    public static AnalysisValidationResult Valid { get; } = new()
    {
        Errors = []
    };

    public static AnalysisValidationResult Invalid(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new AnalysisValidationResult
        {
            Errors = errors
        };
    }
}
