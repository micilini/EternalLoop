using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public sealed class StructuralBranchContext
{
    public string Name { get; init; } = StructuralBranchPolicy.PolicyName;
    public bool Enabled { get; init; }
    public StructuralBranchOptions Options { get; init; } = new();
    public int BeatsPerBar { get; init; }
    public int TotalBeats { get; init; }
    public int VeryShortJumpBeats { get; init; }
    public int ShortJumpBeats { get; init; }
    public int PhraseWindowBeats { get; init; }
    public Dictionary<TimeQuantum, StructuralBeatContext> BeatContexts { get; init; } = [];
    public int StructurallyRejectedBranches { get; set; }
}
