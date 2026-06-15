using EternalLoop.Core.Settings;
using EternalLoop.BranchAnalysis.Core.Runner;
using EternalLoop.Playback.Runtime;

namespace EternalLoop.Core.Runtime;

public sealed class TrackRuntimePackageRebuilder
{
    public TrackRuntimePackage RebuildRuntimeOptions(
        TrackRuntimePackage existing,
        LoopTuningSettings currentTuning,
        int settingsSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(currentTuning);

        TrackRuntimeTuningSnapshot tuningSnapshot = CreateTuningSnapshot(currentTuning);

        return existing with
        {
            Metadata = existing.Metadata with
            {
                SettingsSchemaVersion = settingsSchemaVersion
            },
            Tuning = tuningSnapshot,
            BranchDecisionOptions = CreateBranchDecisionOptions(currentTuning),
            Summary = existing.Summary,
            RuntimeTrack = existing.RuntimeTrack,
            Files = existing.Files,
            IgnoredActiveBranches = existing.IgnoredActiveBranches,
            IgnoredCandidateBranches = existing.IgnoredCandidateBranches
        };
    }

    private static TrackRuntimeTuningSnapshot CreateTuningSnapshot(LoopTuningSettings tuning)
    {
        double similarityThreshold = Clamp(tuning.SimilarityThreshold, 0.65, 0.95);

        return new TrackRuntimeTuningSnapshot(
            string.IsNullOrWhiteSpace(tuning.Preset)
                ? LoopTuningPresetCatalog.BalancedId
                : tuning.Preset,
            similarityThreshold,
            Clamp(tuning.LookaheadDepth, 1, 5),
            Clamp(tuning.MinJumpDistance, 4, 64),
            Clamp(tuning.MaxBranchesPerBeat, 1, 12),
            string.IsNullOrWhiteSpace(tuning.BranchQuantumType) ? "beats" : tuning.BranchQuantumType,
            BranchAnalysisTuningMapper.MapSimilarityToMaxThreshold(similarityThreshold),
            tuning.AnalysisMusicalQuality,
            Clamp(tuning.JumpProbability, 0, 1),
            Clamp(tuning.JumpCooldown, 0, 64),
            Clamp(tuning.FirstPassLinearPlaybackRatio, 0, 1));
    }

    private static BranchDecisionOptions CreateBranchDecisionOptions(LoopTuningSettings tuning)
    {
        return new BranchDecisionOptions
        {
            JumpProbability = Clamp(tuning.JumpProbability, 0, 1),
            JumpCooldownBeats = Math.Max(0, tuning.JumpCooldown),
            FirstPassLinearPlaybackRatio = Clamp(tuning.FirstPassLinearPlaybackRatio, 0, 1),
            EnableJumpShapingKnobs = true,
            NormalizeChanceDeltaByTempo = true,
            WeightedBranchSelection = true,
            RepeatPenalty = 0.35
        };
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value))
        {
            return minimum;
        }

        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
