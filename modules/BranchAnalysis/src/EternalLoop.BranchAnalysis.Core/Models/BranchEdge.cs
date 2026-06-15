using System.Text.Json.Serialization;

namespace EternalLoop.BranchAnalysis.Core.Models;

public sealed class BranchEdge
{
    public int Id { get; set; }
    public TimeQuantum? Source { get; set; }
    public TimeQuantum? Destination { get; set; }
    public double Distance { get; set; }
    public bool Deleted { get; set; }
    public double AcousticDistance { get; set; } = double.NaN;
    public double BranchScore { get; set; } = double.NaN;
    public double StructuralPenalty { get; set; }
    public double StructuralBonus { get; set; }
    public double StructuralBonusDiagnosticOnly { get; set; }
    public double JumpBeatsAbs { get; set; } = double.NaN;
    public double JumpBars { get; set; } = double.NaN;
    public bool SameBarPhase { get; set; }
    public bool SamePhrasePhase4 { get; set; }
    public bool SamePhrasePhase8 { get; set; }
    public bool SamePhrasePhase16 { get; set; }
    public bool SectionChange { get; set; }
    public bool NearStructuralBoundary { get; set; }
    public bool ShortLocalRisk { get; set; }
    public bool LocalLoopRisk { get; set; }
    public string PolicyDecision { get; set; } = "legacy";
    public List<string> PolicyReasons { get; set; } = [];

    [JsonIgnore]
    public TimeQuantum? Src => Source;

    [JsonIgnore]
    public TimeQuantum? Dest => Destination;

    [JsonIgnore]
    public object? Curve { get; set; }
}
