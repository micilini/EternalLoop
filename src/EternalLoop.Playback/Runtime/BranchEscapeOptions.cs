namespace EternalLoop.Playback.Runtime;

public sealed class BranchEscapeOptions
{
    public bool Enabled { get; init; } = true;

    public double EndGuardStartRatio { get; init; } = 0.85;

    public int MinimumBeatsBeforeEndForJumpDestination { get; init; } = 16;

    public int TerminalEscapeLookaheadBeats { get; init; } = 24;

    public bool ForceJumpInEndGuard { get; init; } = true;

    public int MaxEscapeSearchDepth { get; init; } = 3;

    public BranchEscapeOptions Normalize()
    {
        return new BranchEscapeOptions
        {
            Enabled = Enabled,
            EndGuardStartRatio = double.IsNaN(EndGuardStartRatio) || double.IsInfinity(EndGuardStartRatio)
                ? 0.85
                : Math.Clamp(EndGuardStartRatio, 0.50, 0.98),
            MinimumBeatsBeforeEndForJumpDestination = MinimumBeatsBeforeEndForJumpDestination < 1
                ? 16
                : MinimumBeatsBeforeEndForJumpDestination,
            TerminalEscapeLookaheadBeats = TerminalEscapeLookaheadBeats < 1
                ? 24
                : TerminalEscapeLookaheadBeats,
            ForceJumpInEndGuard = ForceJumpInEndGuard,
            MaxEscapeSearchDepth = MaxEscapeSearchDepth < 1
                ? 3
                : MaxEscapeSearchDepth
        };
    }
}
