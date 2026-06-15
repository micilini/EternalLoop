using EternalLoop.BranchAnalysis.Core.Config;

namespace EternalLoop.BranchAnalysis.Core.Runner;

public sealed class BranchAnalysisOptions
{
    public string AnalysisRoot { get; set; } = string.Empty;
    public string OutputRoot { get; set; } = string.Empty;
    public string QuantumType { get; set; } = string.Empty;
    public double SimilarityThreshold { get; set; }
    public int LookaheadDepth { get; set; }
    public int MinJumpDistance { get; set; }
    public int MaxBranches { get; set; }
    public int MaxThreshold { get; set; }
    public bool StructuralPolicy { get; set; }
    public bool AntiLocalLoopPolicy { get; set; }
    public string ShortBranchPolicy { get; set; } = string.Empty;
    public int VeryShortBars { get; set; }
    public int ShortBars { get; set; }
    public int PhraseBars { get; set; }
    public int LocalWindowBars { get; set; }
    public int MaxShortLocalBranchesPerCluster { get; set; }
    public bool Force { get; set; }
    public bool Pretty { get; set; }
    public bool Quiet { get; set; }

    public static BranchAnalysisOptions CreateDefault()
    {
        return new BranchAnalysisOptions
        {
            AnalysisRoot = BranchAnalysisDefaults.AnalysisRoot,
            OutputRoot = BranchAnalysisDefaults.OutputRoot,
            QuantumType = BranchAnalysisDefaults.QuantumType,
            SimilarityThreshold = BranchAnalysisDefaults.SimilarityThreshold,
            LookaheadDepth = BranchAnalysisDefaults.LookaheadDepth,
            MinJumpDistance = BranchAnalysisDefaults.MinJumpDistance,
            MaxBranches = BranchAnalysisDefaults.MaxBranches,
            MaxThreshold = BranchAnalysisDefaults.MaxThreshold,
            StructuralPolicy = BranchAnalysisDefaults.StructuralPolicy,
            AntiLocalLoopPolicy = BranchAnalysisDefaults.AntiLocalLoopPolicy,
            ShortBranchPolicy = BranchAnalysisDefaults.ShortBranchPolicy,
            VeryShortBars = BranchAnalysisDefaults.VeryShortBars,
            ShortBars = BranchAnalysisDefaults.ShortBars,
            PhraseBars = BranchAnalysisDefaults.PhraseBars,
            LocalWindowBars = BranchAnalysisDefaults.LocalWindowBars,
            MaxShortLocalBranchesPerCluster = BranchAnalysisDefaults.MaxShortLocalBranchesPerCluster,
            Force = BranchAnalysisDefaults.Force,
            Pretty = BranchAnalysisDefaults.Pretty,
            Quiet = BranchAnalysisDefaults.Quiet
        };
    }
}
