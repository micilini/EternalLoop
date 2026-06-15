using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class StructuralBranchOptions
{
    public bool StructuralPolicy { get; init; } = BranchAnalysisDefaults.StructuralPolicy;
    public bool AntiLocalLoopPolicy { get; init; } = BranchAnalysisDefaults.AntiLocalLoopPolicy;
    public string ShortBranchPolicy { get; init; } = BranchAnalysisDefaults.ShortBranchPolicy;
    public int VeryShortBars { get; init; } = BranchAnalysisDefaults.VeryShortBars;
    public int ShortBars { get; init; } = BranchAnalysisDefaults.ShortBars;
    public int PhraseBars { get; init; } = BranchAnalysisDefaults.PhraseBars;
    public int LocalWindowBars { get; init; } = BranchAnalysisDefaults.LocalWindowBars;
    public int MaxShortLocalBranchesPerCluster { get; init; } = BranchAnalysisDefaults.MaxShortLocalBranchesPerCluster;
    public double ExceptionalAcousticDistance { get; init; } = StructuralBranchPolicy.DefaultExceptionalAcousticDistance;

    public static StructuralBranchOptions FromBranchGraphData(BranchGraphData data)
    {
        return Normalize(
            data.StructuralPolicy,
            data.AntiLocalLoopPolicy,
            data.ShortBranchPolicy,
            data.VeryShortBars,
            data.ShortBars,
            data.PhraseBars,
            data.LocalWindowBars,
            data.MaxShortLocalBranchesPerCluster,
            null);
    }

    public static StructuralBranchOptions FromNearestNeighborOptions(NearestNeighborOptions options)
    {
        return Normalize(
            options.StructuralPolicy,
            options.AntiLocalLoopPolicy,
            options.ShortBranchPolicy,
            options.VeryShortBars,
            options.ShortBars,
            options.PhraseBars,
            options.LocalWindowBars,
            options.MaxShortLocalBranchesPerCluster,
            null);
    }

    public static StructuralBranchOptions Normalize(
        bool? structuralPolicy = null,
        bool? antiLocalLoopPolicy = null,
        string? shortBranchPolicy = null,
        int? veryShortBars = null,
        int? shortBars = null,
        int? phraseBars = null,
        int? localWindowBars = null,
        int? maxShortLocalBranchesPerCluster = null,
        double? exceptionalAcousticDistance = null)
    {
        return new StructuralBranchOptions
        {
            StructuralPolicy = structuralPolicy ?? BranchAnalysisDefaults.StructuralPolicy,
            AntiLocalLoopPolicy = antiLocalLoopPolicy ?? BranchAnalysisDefaults.AntiLocalLoopPolicy,
            ShortBranchPolicy = string.IsNullOrWhiteSpace(shortBranchPolicy)
                ? BranchAnalysisDefaults.ShortBranchPolicy
                : shortBranchPolicy,
            VeryShortBars = PositiveOrDefault(veryShortBars, BranchAnalysisDefaults.VeryShortBars),
            ShortBars = PositiveOrDefault(shortBars, BranchAnalysisDefaults.ShortBars),
            PhraseBars = PositiveOrDefault(phraseBars, BranchAnalysisDefaults.PhraseBars),
            LocalWindowBars = PositiveOrDefault(localWindowBars, BranchAnalysisDefaults.LocalWindowBars),
            MaxShortLocalBranchesPerCluster = NonNegativeOrDefault(
                maxShortLocalBranchesPerCluster,
                BranchAnalysisDefaults.MaxShortLocalBranchesPerCluster),
            ExceptionalAcousticDistance = exceptionalAcousticDistance is > 0
                && double.IsFinite(exceptionalAcousticDistance.Value)
                    ? exceptionalAcousticDistance.Value
                    : StructuralBranchPolicy.DefaultExceptionalAcousticDistance
        };
    }

    private static int PositiveOrDefault(int? value, int fallback)
    {
        return value is > 0 ? value.Value : fallback;
    }

    private static int NonNegativeOrDefault(int? value, int fallback)
    {
        return value is >= 0 ? value.Value : fallback;
    }
}
