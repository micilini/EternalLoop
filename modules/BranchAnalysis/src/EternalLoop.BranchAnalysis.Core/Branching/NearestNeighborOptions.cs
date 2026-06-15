using EternalLoop.BranchAnalysis.Core.Runner;
using EternalLoop.BranchAnalysis.Core.Config;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class NearestNeighborOptions
{
    public int? MaxBranches { get; init; }
    public double? SimilarityThreshold { get; init; }
    public int? LookaheadDepth { get; init; }
    public int? MinJumpDistance { get; init; }
    public double? MaxBranchThreshold { get; init; }
    public double? CurrentThreshold { get; init; }
    public double? ComputedThreshold { get; init; }
    public bool? JustBackwards { get; init; }
    public bool? JustLongBranches { get; init; }
    public double? MinLongBranch { get; init; }
    public bool? AddLastEdge { get; init; }
    public bool? RemoveSequentialBranches { get; init; }
    public bool? StructuralPolicy { get; init; }
    public bool? AntiLocalLoopPolicy { get; init; }
    public string? ShortBranchPolicy { get; init; }
    public int? VeryShortBars { get; init; }
    public int? ShortBars { get; init; }
    public int? PhraseBars { get; init; }
    public int? LocalWindowBars { get; init; }
    public int? MaxShortLocalBranchesPerCluster { get; init; }

    public static NearestNeighborOptions FromBranchAnalysisOptions(BranchAnalysisOptions options)
    {
        return new NearestNeighborOptions
        {
            SimilarityThreshold = options.SimilarityThreshold,
            LookaheadDepth = options.LookaheadDepth,
            MinJumpDistance = options.MinJumpDistance,
            MaxBranches = options.MaxBranches,
            MaxBranchThreshold = options.MaxThreshold,
            JustLongBranches = options.MinJumpDistance > BranchAnalysisDefaults.MinJumpDistance,
            MinLongBranch = options.MinJumpDistance,
            StructuralPolicy = options.StructuralPolicy,
            AntiLocalLoopPolicy = options.AntiLocalLoopPolicy,
            ShortBranchPolicy = options.ShortBranchPolicy,
            VeryShortBars = options.VeryShortBars,
            ShortBars = options.ShortBars,
            PhraseBars = options.PhraseBars,
            LocalWindowBars = options.LocalWindowBars,
            MaxShortLocalBranchesPerCluster = options.MaxShortLocalBranchesPerCluster
        };
    }
}
