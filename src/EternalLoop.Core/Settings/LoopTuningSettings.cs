namespace EternalLoop.Core.Settings;

public sealed class LoopTuningSettings
{
    public string Preset { get; set; } = LoopTuningPresetCatalog.BalancedId;

    public double SimilarityThreshold { get; set; } = 0.86;

    public int LookaheadDepth { get; set; } = 1;

    public int MinJumpDistance { get; set; } = 4;

    public int MaxBranchesPerBeat { get; set; } = 4;

    public double JumpProbability { get; set; } = 0.22;

    public int JumpCooldown { get; set; } = 12;

    public double FirstPassLinearPlaybackRatio { get; set; } = 0.78;

    public string BranchQuantumType { get; set; } = "beats";

    public int BranchMaxThreshold { get; set; } = 80;

    public bool AnalysisMusicalQuality { get; set; } = true;

    public static LoopTuningSettings Balanced()
    {
        var settings = new LoopTuningSettings();
        LoopTuningPresetCatalog.ApplyPreset(
            settings,
            LoopTuningPresetCatalog.GetById(LoopTuningPresetCatalog.BalancedId));

        return settings;
    }
}
