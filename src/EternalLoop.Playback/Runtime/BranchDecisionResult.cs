using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class BranchDecisionResult
{
    public required RuntimeBeat SourceBeat { get; init; }

    public required RuntimeBeat SeedBeat { get; init; }

    public required RuntimeBeat NextBeat { get; init; }

    public RuntimeBranchEdge? Branch { get; init; }

    public bool UsedBranch => Branch is not null;

    public double ChanceBeforeDecision { get; init; }

    public double ChanceAfterDecision { get; init; }

    public double RandomValue { get; init; }

    public string Reason { get; init; } = string.Empty;

    public bool WasBlockedByEscapeGuard { get; init; }

    public string EscapeGuardReason { get; init; } = string.Empty;

    public int BlockedBranchCount { get; init; }

    public bool ForcedEndGuardJump { get; init; }

    public bool BlockedByFirstPass { get; init; }

    public bool BlockedByCooldown { get; init; }

    public bool BringItHomeActive { get; init; }

    public int CandidateCountConsidered { get; init; }
}
