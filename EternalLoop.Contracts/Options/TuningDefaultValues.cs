namespace EternalLoop.Contracts.Options;

public static class TuningDefaultValues
{
    public const int SettingsSchemaVersion = 3;

    public const double DefaultVolume = 1.0;

    public const double SimilarityThreshold = 0.86;
    public const int LookaheadDepth = 4;
    public const int MinJumpDistance = 20;
    public const int MaxBranchesPerBeat = 3;
    public const double JumpProbability = 0.22;
    public const int JumpCooldown = 12;
    public const double FirstPassLinearPlaybackRatio = 0.78;

    public const double TimbreWeight = 0.45;
    public const double PitchWeight = 0.35;
    public const double LoudnessWeight = 0.20;
    public const double BarPositionWeight = 0.18;
    public const int PhraseValidationLookaheadDepth = 6;
    public const double PhraseValidationThresholdMargin = 0.02;

    public const bool UseAiSimilarity = true;
    public const double AiRejectionThreshold = 0.58;
    public const double AiPenaltyStartThreshold = 0.72;
    public const double AiPenaltyStrength = 0.22;
    public const int AiBeatContextBefore = 1;
    public const int AiBeatContextAfter = 2;

    public const int MinLookaheadDepth = 1;
    public const int MaxLookaheadDepth = 8;
    public const int MinJumpDistanceLimit = 1;
    public const int MaxJumpDistanceLimit = 128;
    public const int MinBranchesPerBeat = 1;
    public const int MaxBranchesPerBeatLimit = 24;
    public const int MinJumpCooldown = 0;
    public const int MaxJumpCooldown = 64;
    public const double MinRatio = 0.0;
    public const double MaxRatio = 0.95;
    public const double MinProbability = 0.0;
    public const double MaxProbability = 1.0;

    public const int MinBeatsBeforeFirstJump = 16;
    public const int SteeringLookaheadDepth = 5;
    public const double EndGuardStartRatio = 0.88;
    public const int MinimumBeatsBeforeEndForJumpDestination = 24;
    public const int TerminalEscapeLookaheadBeats = 32;
    public const int RepeatedJumpAvoidancePasses = 2;
}
