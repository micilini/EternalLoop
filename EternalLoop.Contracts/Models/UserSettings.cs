using System.Collections.Generic;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Contracts.Models;

public sealed class UserSettings
{
    public int SettingsSchemaVersion { get; set; } = TuningDefaultValues.SettingsSchemaVersion;

    public string Theme { get; set; } = "Dark";

    public float Volume { get; set; } = (float)TuningDefaultValues.DefaultVolume;

    public string Preset { get; set; } = TuningPresetCatalog.BalancedId;

    public double SimilarityThreshold { get; set; } = TuningDefaultValues.SimilarityThreshold;

    public int LookaheadDepth { get; set; } = TuningDefaultValues.LookaheadDepth;

    public int MinJumpDistance { get; set; } = TuningDefaultValues.MinJumpDistance;

    public int MaxBranchesPerBeat { get; set; } = TuningDefaultValues.MaxBranchesPerBeat;

    public double JumpProbability { get; set; } = TuningDefaultValues.JumpProbability;

    public int JumpCooldown { get; set; } = TuningDefaultValues.JumpCooldown;

    public double FirstPassLinearPlaybackRatio { get; set; } = TuningDefaultValues.FirstPassLinearPlaybackRatio;

    public bool UseAiSimilarity { get; set; } = TuningDefaultValues.UseAiSimilarity;

    public string? LastOpenedFile { get; set; }

    public List<string> RecentFiles { get; set; } = new();
}
