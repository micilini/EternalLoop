namespace EternalLoop.App.Services;

public sealed class TuningApplyResult
{
    public required bool GraphReloaded { get; init; }

    public required int BranchCount { get; init; }

    public required string Message { get; init; }
}
