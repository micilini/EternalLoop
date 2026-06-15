namespace EternalLoop.Core.Settings;

public static class LoopTuningImpactClassifier
{
    public static LoopTuningImpact Compare(
        LoopTuningSettings previous,
        LoopTuningSettings current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        if (previous.AnalysisMusicalQuality != current.AnalysisMusicalQuality)
        {
            return LoopTuningImpact.Analysis;
        }

        if (!DoubleEquals(previous.SimilarityThreshold, current.SimilarityThreshold)
            || previous.LookaheadDepth != current.LookaheadDepth
            || previous.MinJumpDistance != current.MinJumpDistance
            || previous.MaxBranchesPerBeat != current.MaxBranchesPerBeat
            || !StringEquals(previous.BranchQuantumType, current.BranchQuantumType)
            || previous.BranchMaxThreshold != current.BranchMaxThreshold)
        {
            return LoopTuningImpact.Branches;
        }

        if (!DoubleEquals(previous.JumpProbability, current.JumpProbability)
            || previous.JumpCooldown != current.JumpCooldown
            || !DoubleEquals(previous.FirstPassLinearPlaybackRatio, current.FirstPassLinearPlaybackRatio))
        {
            return LoopTuningImpact.RuntimeOnly;
        }

        return LoopTuningImpact.None;
    }

    private static bool DoubleEquals(double left, double right)
    {
        return left.Equals(right);
    }

    private static bool StringEquals(string? left, string? right)
    {
        return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
    }
}
