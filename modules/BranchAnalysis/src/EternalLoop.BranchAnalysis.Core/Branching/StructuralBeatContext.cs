using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class StructuralBeatContext
{
    public required TimeQuantum Quantum { get; init; }
    public int BeatIndex { get; init; }
    public int BarIndex { get; init; }
    public int SectionIndex { get; init; }
    public int BeatInBar { get; init; }
    public int BarInSection { get; init; }
    public int BeatsPerBar { get; init; }
    public int Phrase4Index { get; init; }
    public int Phrase8Index { get; init; }
    public int Phrase16Index { get; init; }
    public int Phrase4Phase { get; init; }
    public int Phrase8Phase { get; init; }
    public int Phrase16Phase { get; init; }
    public bool NearSectionBoundary { get; init; }
    public bool NearBarBoundary { get; init; }
    public double Confidence { get; init; }
}
