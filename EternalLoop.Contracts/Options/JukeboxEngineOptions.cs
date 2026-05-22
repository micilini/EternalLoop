using EternalLoop.Contracts.Enums;

namespace EternalLoop.Contracts.Options;

public sealed class JukeboxEngineOptions
{
    public double JumpProbability { get; init; } = 0.3;

    public int MinBeatsBeforeFirstJump { get; init; } = TuningDefaultValues.MinBeatsBeforeFirstJump;

    public int JumpCooldown { get; init; } = 8;

    public int SteeringLookaheadDepth { get; init; } = TuningDefaultValues.SteeringLookaheadDepth;

    public JumpStrategy Strategy { get; init; } = JumpStrategy.LeastPlayed;

    public double FirstPassLinearPlaybackRatio { get; init; } = 0.75;

    public double EndGuardStartRatio { get; init; } = TuningDefaultValues.EndGuardStartRatio;

    public int MinimumBeatsBeforeEndForJumpDestination { get; init; } =
        TuningDefaultValues.MinimumBeatsBeforeEndForJumpDestination;

    public int TerminalEscapeLookaheadBeats { get; init; } = TuningDefaultValues.TerminalEscapeLookaheadBeats;

    public bool ForceJumpInEndGuard { get; init; } = true;

    public int RepeatedJumpAvoidancePasses { get; init; } = TuningDefaultValues.RepeatedJumpAvoidancePasses;

    public bool AllowRepeatedJumpForTerminalEscape { get; init; } = true;
}
