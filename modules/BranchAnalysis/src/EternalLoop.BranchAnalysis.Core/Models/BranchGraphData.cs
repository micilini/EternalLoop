using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Branching;
using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public sealed class BranchGraphData
{
    public List<BranchEdge> AllEdges { get; set; } = [];
    public double SimilarityThreshold { get; set; } = BranchAnalysisDefaults.SimilarityThreshold;
    public int LookaheadDepth { get; set; } = BranchAnalysisDefaults.LookaheadDepth;
    public int MinJumpDistance { get; set; } = BranchAnalysisDefaults.MinJumpDistance;
    public int MaxBranches { get; set; } = 4;
    public double MaxBranchThreshold { get; set; } = 80;
    public double CurrentThreshold { get; set; } = 80;
    public double ComputedThreshold { get; set; } = 80;
    public bool JustBackwards { get; set; }
    public bool JustLongBranches { get; set; }
    public double MinLongBranch { get; set; }
    public bool AddLastEdge { get; set; } = true;
    public bool RemoveSequentialBranches { get; set; }
    public bool StructuralPolicy { get; set; } = BranchAnalysisDefaults.StructuralPolicy;
    public bool AntiLocalLoopPolicy { get; set; } = BranchAnalysisDefaults.AntiLocalLoopPolicy;
    public string ShortBranchPolicy { get; set; } = BranchAnalysisDefaults.ShortBranchPolicy;
    public int VeryShortBars { get; set; } = BranchAnalysisDefaults.VeryShortBars;
    public int ShortBars { get; set; } = BranchAnalysisDefaults.ShortBars;
    public int PhraseBars { get; set; } = BranchAnalysisDefaults.PhraseBars;
    public int LocalWindowBars { get; set; } = BranchAnalysisDefaults.LocalWindowBars;
    public int MaxShortLocalBranchesPerCluster { get; set; } = BranchAnalysisDefaults.MaxShortLocalBranchesPerCluster;
    public int StructurallyRejectedBranches { get; set; }
    public int AntiMRemovedBranches { get; set; }
    public int LocalLoopRiskBranches { get; set; }
    public List<BranchEdge> DeletedEdges { get; set; } = [];
    public int DeletedEdgeCount { get; set; }
    public int BranchCount { get; set; }
    public int LastBranchPoint { get; set; }
    public double LongestReach { get; set; }
    public int TotalBeats { get; set; }

    [JsonIgnore]
    public StructuralBranchContext? StructuralContext { get; set; }
}
