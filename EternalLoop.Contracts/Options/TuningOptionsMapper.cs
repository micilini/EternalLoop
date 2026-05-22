using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;

namespace EternalLoop.Contracts.Options;

public static class TuningOptionsMapper
{
    public static BranchFindingOptions ToBranchFindingOptions(UserSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return new BranchFindingOptions
        {
            SimilarityThreshold = Clamp(
                settings.SimilarityThreshold,
                TuningDefaultValues.MinProbability,
                TuningDefaultValues.MaxProbability),
            LookaheadDepth = Clamp(
                settings.LookaheadDepth,
                TuningDefaultValues.MinLookaheadDepth,
                TuningDefaultValues.MaxLookaheadDepth),
            MinJumpDistance = Clamp(
                settings.MinJumpDistance,
                TuningDefaultValues.MinJumpDistanceLimit,
                TuningDefaultValues.MaxJumpDistanceLimit),
            MaxBranchesPerBeat = Clamp(
                settings.MaxBranchesPerBeat,
                TuningDefaultValues.MinBranchesPerBeat,
                TuningDefaultValues.MaxBranchesPerBeatLimit),
            TimbreWeight = TuningDefaultValues.TimbreWeight,
            PitchWeight = TuningDefaultValues.PitchWeight,
            LoudnessWeight = TuningDefaultValues.LoudnessWeight,
            BarPositionWeight = TuningDefaultValues.BarPositionWeight,
            ContinuationLookaheadDepth = TuningDefaultValues.PhraseValidationLookaheadDepth,
            ContinuationThresholdMargin = TuningDefaultValues.PhraseValidationThresholdMargin,
            UseAiSimilarity = settings.UseAiSimilarity,
            AiRejectionThreshold = TuningDefaultValues.AiRejectionThreshold,
            AiPenaltyStartThreshold = TuningDefaultValues.AiPenaltyStartThreshold,
            AiPenaltyStrength = TuningDefaultValues.AiPenaltyStrength
        };
    }

    public static JukeboxEngineOptions ToJukeboxEngineOptions(UserSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return new JukeboxEngineOptions
        {
            JumpProbability = Clamp(
                settings.JumpProbability,
                TuningDefaultValues.MinProbability,
                TuningDefaultValues.MaxProbability),
            MinBeatsBeforeFirstJump = TuningDefaultValues.MinBeatsBeforeFirstJump,
            JumpCooldown = Clamp(
                settings.JumpCooldown,
                TuningDefaultValues.MinJumpCooldown,
                TuningDefaultValues.MaxJumpCooldown),
            SteeringLookaheadDepth = TuningDefaultValues.SteeringLookaheadDepth,
            Strategy = JumpStrategy.LeastPlayed,
            FirstPassLinearPlaybackRatio = Clamp(
                settings.FirstPassLinearPlaybackRatio,
                TuningDefaultValues.MinRatio,
                TuningDefaultValues.MaxRatio),
            EndGuardStartRatio = TuningDefaultValues.EndGuardStartRatio,
            MinimumBeatsBeforeEndForJumpDestination = TuningDefaultValues.MinimumBeatsBeforeEndForJumpDestination,
            TerminalEscapeLookaheadBeats = TuningDefaultValues.TerminalEscapeLookaheadBeats,
            ForceJumpInEndGuard = true,
            RepeatedJumpAvoidancePasses = TuningDefaultValues.RepeatedJumpAvoidancePasses,
            AllowRepeatedJumpForTerminalEscape = true
        };
    }

    public static AiAnalysisOptions ToAiAnalysisOptions(UserSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return new AiAnalysisOptions
        {
            IsEnabled = settings.UseAiSimilarity,
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            RejectionThreshold = TuningDefaultValues.AiRejectionThreshold,
            PenaltyStartThreshold = TuningDefaultValues.AiPenaltyStartThreshold,
            PenaltyStrength = TuningDefaultValues.AiPenaltyStrength,
            BeatContextBefore = TuningDefaultValues.AiBeatContextBefore,
            BeatContextAfter = TuningDefaultValues.AiBeatContextAfter
        };
    }

    public static void ApplyPreset(UserSettings settings, TuningPresetDefinition preset)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (preset is null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        settings.Preset = preset.Id;
        settings.SimilarityThreshold = preset.SimilarityThreshold;
        settings.LookaheadDepth = preset.LookaheadDepth;
        settings.MinJumpDistance = preset.MinJumpDistance;
        settings.MaxBranchesPerBeat = preset.MaxBranchesPerBeat;
        settings.JumpProbability = preset.JumpProbability;
        settings.JumpCooldown = preset.JumpCooldown;
        settings.FirstPassLinearPlaybackRatio = preset.FirstPassLinearPlaybackRatio;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
