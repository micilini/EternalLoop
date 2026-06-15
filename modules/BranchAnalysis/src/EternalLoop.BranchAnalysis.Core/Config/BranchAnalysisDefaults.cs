namespace EternalLoop.BranchAnalysis.Core.Config;

public static class BranchAnalysisDefaults
{
    public const string AnalysisRoot = @"..\examples\2. audio-analysis";
    public const string OutputRoot = @"..\examples\3. branchs-analysis";
    public const string QuantumType = "beats";
    public const double SimilarityThreshold = 0.86;
    public const int LookaheadDepth = 1;
    public const int MinJumpDistance = 4;
    public const int MaxBranches = 4;
    public const int MaxThreshold = 80;
    public const bool StructuralPolicy = true;
    public const bool AntiLocalLoopPolicy = true;
    public const string ShortBranchPolicy = "structural-gated";
    public const int VeryShortBars = 2;
    public const int ShortBars = 4;
    public const int PhraseBars = 8;
    public const int LocalWindowBars = 2;
    public const int MaxShortLocalBranchesPerCluster = 1;
    public const bool Force = true;
    public const bool Pretty = true;
    public const bool Quiet = false;
}
