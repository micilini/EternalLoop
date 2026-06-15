namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class StructuralBranchScore
{
    public double AcousticDistance { get; init; }
    public double StructuralPenalty { get; init; }
    public double StructuralBonus { get; init; }
    public double StructuralBonusDiagnosticOnly { get; init; }
    public double BranchScore { get; init; }
    public double JumpBeatsAbs { get; init; }
    public double JumpBars { get; init; }
    public bool SameBarPhase { get; init; }
    public bool SamePhrasePhase4 { get; init; }
    public bool SamePhrasePhase8 { get; init; }
    public bool SamePhrasePhase16 { get; init; }
    public bool SectionChange { get; init; }
    public bool NearStructuralBoundary { get; init; }
    public bool ShortLocalRisk { get; init; }
    public bool LocalLoopRisk { get; init; }
    public string PolicyDecision { get; init; } = "accepted";
    public List<string> PolicyReasons { get; init; } = [];
}
