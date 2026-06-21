namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed class BeatGridGuardrailResult
{
    private BeatGridGuardrailResult(bool isValid, string reason)
    {
        IsValid = isValid;
        Reason = reason;
    }

    public bool IsValid { get; }

    public string Reason { get; }

    public static BeatGridGuardrailResult Valid()
    {
        return new BeatGridGuardrailResult(true, "ok");
    }

    public static BeatGridGuardrailResult Invalid(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new BeatGridGuardrailResult(false, reason);
    }
}