using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class BranchDecisionEngine
{
    private readonly BranchDecisionOptions _options;
    private readonly IBranchRandomProvider _randomProvider;
    private readonly BranchEscapeGuard _escapeGuard;
    private RuntimeLinearBeatIndex? _linearBeatIndex;
    private RuntimeBeat? _linearBeatIndexFirstBeat;
    private RuntimeBranchOrder? _branchOrder;
    private RuntimeLinearBeatIndex? _branchOrderLinearBeatIndex;
    private double _branchChance;

    public BranchDecisionEngine(
        BranchDecisionOptions? options = null,
        IBranchRandomProvider? randomProvider = null,
        BranchEscapeGuard? escapeGuard = null)
    {
        _options = Normalize(options ?? new BranchDecisionOptions());
        _randomProvider = randomProvider ?? new DefaultBranchRandomProvider();
        _escapeGuard = escapeGuard ?? new BranchEscapeGuard(_options.EscapeOptions);
        _branchChance = _options.MinRandomBranchChance;
    }

    public double BranchChance => _branchChance;

    public BranchDecisionResult DecideNextBeat(RuntimeBeat currentBeat, RuntimeBeat firstBeat)
    {
        ArgumentNullException.ThrowIfNull(currentBeat);
        ArgumentNullException.ThrowIfNull(firstBeat);

        return DecideNextBeat(currentBeat, GetLinearBeatIndex(firstBeat));
    }

    public BranchDecisionResult DecideNextBeat(RuntimeBeat currentBeat, RuntimeLinearBeatIndex linearBeatIndex)
    {
        ArgumentNullException.ThrowIfNull(currentBeat);
        ArgumentNullException.ThrowIfNull(linearBeatIndex);

        if (linearBeatIndex.Count == 0)
        {
            throw new ArgumentException("Linear beat index must contain beats.", nameof(linearBeatIndex));
        }

        RuntimeBeat firstBeat = linearBeatIndex.Beats[0];
        RuntimeBeat seedBeat = currentBeat.Next ?? firstBeat;
        RuntimeBranchOrder branchOrder = GetBranchOrder(linearBeatIndex);
        double chanceBeforeDecision = _branchChance;

        if (!_options.InfiniteMode)
        {
            return CreateResult(currentBeat, seedBeat, seedBeat, null, chanceBeforeDecision, _branchChance, 1, "Infinite mode disabled");
        }

        BranchCandidateSelection selection = SelectSafeBranch(currentBeat, seedBeat, linearBeatIndex, branchOrder);
        RuntimeBranchEdge? branch = selection.Branch;

        if (branch is null)
        {
            IncreaseChance();
            return CreateResult(
                currentBeat,
                seedBeat,
                seedBeat,
                null,
                chanceBeforeDecision,
                _branchChance,
                1,
                selection.BlockedCount > 0 ? "Escape guard blocked branch" : "No valid branch",
                selection.BlockedCount > 0,
                selection.GuardReason,
                selection.BlockedCount,
                false);
        }

        bool shouldForceEndGuardJump = _options.EscapeOptions.Enabled
            && _options.EscapeOptions.ForceJumpInEndGuard
            && _escapeGuard.IsInEndZone(seedBeat, linearBeatIndex);

        if (shouldForceEndGuardJump)
        {
            AdvanceBranchOrder(seedBeat, branch, branchOrder);
            Reset();
            return CreateResult(
                currentBeat,
                seedBeat,
                branch.DestinationBeat,
                branch,
                chanceBeforeDecision,
                _branchChance,
                1,
                "Forced safe branch in end guard",
                false,
                selection.GuardReason,
                selection.BlockedCount,
                true);
        }

        double randomValue = _randomProvider.NextDouble();

        if (_branchChance > 0 && randomValue <= _branchChance)
        {
            AdvanceBranchOrder(seedBeat, branch, branchOrder);
            Reset();
            return CreateResult(
                currentBeat,
                seedBeat,
                branch.DestinationBeat,
                branch,
                chanceBeforeDecision,
                _branchChance,
                randomValue,
                "Branch selected",
                false,
                selection.GuardReason,
                selection.BlockedCount,
                false);
        }

        IncreaseChance();
        return CreateResult(
            currentBeat,
            seedBeat,
            seedBeat,
            null,
            chanceBeforeDecision,
            _branchChance,
            randomValue,
            "Random rejected branch",
            false,
            selection.GuardReason,
            selection.BlockedCount,
            false);
    }

    public void Reset()
    {
        _branchChance = _options.MinRandomBranchChance;
    }

    private BranchCandidateSelection SelectSafeBranch(
        RuntimeBeat currentBeat,
        RuntimeBeat seedBeat,
        RuntimeLinearBeatIndex linearBeatIndex,
        RuntimeBranchOrder branchOrder)
    {
        int blockedCount = 0;
        string guardReason = string.Empty;
        IReadOnlyList<RuntimeBranchEdge> candidates = branchOrder.GetCandidates(seedBeat);

        for (int index = 0; index < candidates.Count; index++)
        {
            RuntimeBranchEdge branch = candidates[index];

            if (!IsBasicValidBranch(currentBeat, seedBeat, branch, linearBeatIndex))
            {
                continue;
            }

            BranchEscapeResult escapeResult = _escapeGuard.Evaluate(currentBeat, seedBeat, branch, linearBeatIndex);

            if (escapeResult.IsSafe)
            {
                return new BranchCandidateSelection(branch, blockedCount, guardReason);
            }

            blockedCount++;
            guardReason = escapeResult.Reason;
        }

        return new BranchCandidateSelection(null, blockedCount, guardReason);
    }

    private static bool IsBasicValidBranch(
        RuntimeBeat currentBeat,
        RuntimeBeat seedBeat,
        RuntimeBranchEdge branch,
        RuntimeLinearBeatIndex linearBeatIndex)
    {
        return !branch.Deleted
            && double.IsFinite(branch.Distance)
            && branch.DestinationBeat is not null
            && !ReferenceEquals(branch.DestinationBeat, seedBeat)
            && !ReferenceEquals(branch.DestinationBeat, currentBeat)
            && linearBeatIndex.Contains(branch.DestinationBeat);
    }

    private void AdvanceBranchOrder(RuntimeBeat seedBeat, RuntimeBranchEdge branch, RuntimeBranchOrder branchOrder)
    {
        if (!_options.RotateBranches)
        {
            return;
        }

        branchOrder.MoveToEnd(seedBeat, branch);
    }

    private void IncreaseChance()
    {
        _branchChance = Math.Min(_options.MaxRandomBranchChance, _branchChance + _options.RandomBranchChanceDelta);
    }

    private RuntimeLinearBeatIndex GetLinearBeatIndex(RuntimeBeat firstBeat)
    {
        if (_linearBeatIndex is not null && ReferenceEquals(_linearBeatIndexFirstBeat, firstBeat))
        {
            return _linearBeatIndex;
        }

        _linearBeatIndex = RuntimeLinearBeatIndex.FromFirstBeat(firstBeat);
        _linearBeatIndexFirstBeat = firstBeat;
        return _linearBeatIndex;
    }

    private RuntimeBranchOrder GetBranchOrder(RuntimeLinearBeatIndex linearBeatIndex)
    {
        if (_branchOrder is not null && ReferenceEquals(_branchOrderLinearBeatIndex, linearBeatIndex))
        {
            return _branchOrder;
        }

        _branchOrder = RuntimeBranchOrder.FromLinearBeatIndex(linearBeatIndex);
        _branchOrderLinearBeatIndex = linearBeatIndex;
        return _branchOrder;
    }

    private static BranchDecisionResult CreateResult(
        RuntimeBeat sourceBeat,
        RuntimeBeat seedBeat,
        RuntimeBeat nextBeat,
        RuntimeBranchEdge? branch,
        double chanceBeforeDecision,
        double chanceAfterDecision,
        double randomValue,
        string reason)
    {
        return CreateResult(
            sourceBeat,
            seedBeat,
            nextBeat,
            branch,
            chanceBeforeDecision,
            chanceAfterDecision,
            randomValue,
            reason,
            false,
            string.Empty,
            0,
            false);
    }

    private static BranchDecisionResult CreateResult(
        RuntimeBeat sourceBeat,
        RuntimeBeat seedBeat,
        RuntimeBeat nextBeat,
        RuntimeBranchEdge? branch,
        double chanceBeforeDecision,
        double chanceAfterDecision,
        double randomValue,
        string reason,
        bool wasBlockedByEscapeGuard,
        string escapeGuardReason,
        int blockedBranchCount,
        bool forcedEndGuardJump)
    {
        return new BranchDecisionResult
        {
            SourceBeat = sourceBeat,
            SeedBeat = seedBeat,
            NextBeat = nextBeat,
            Branch = branch,
            ChanceBeforeDecision = chanceBeforeDecision,
            ChanceAfterDecision = chanceAfterDecision,
            RandomValue = randomValue,
            Reason = reason,
            WasBlockedByEscapeGuard = wasBlockedByEscapeGuard,
            EscapeGuardReason = escapeGuardReason,
            BlockedBranchCount = blockedBranchCount,
            ForcedEndGuardJump = forcedEndGuardJump
        };
    }

    private static BranchDecisionOptions Normalize(BranchDecisionOptions options)
    {
        const double defaultMin = 0.18;
        const double defaultMax = 0.50;
        const double defaultDelta = 0.018;
        const double defaultJumpProbability = 0.22;
        const int defaultJumpCooldown = 12;
        const double defaultFirstPassRatio = 0.78;

        double min = IsFinite(options.MinRandomBranchChance)
            ? Math.Clamp(options.MinRandomBranchChance, 0, 1)
            : defaultMin;
        double max = IsFinite(options.MaxRandomBranchChance)
            ? Math.Clamp(options.MaxRandomBranchChance, 0, 1)
            : defaultMax;

        if (max < min)
        {
            (min, max) = (max, min);
        }

        double delta = double.IsNaN(options.RandomBranchChanceDelta)
            || double.IsInfinity(options.RandomBranchChanceDelta)
            || options.RandomBranchChanceDelta < 0
                ? defaultDelta
                : options.RandomBranchChanceDelta;

        double jumpProbability = IsFinite(options.JumpProbability)
            ? Math.Clamp(options.JumpProbability, 0, 1)
            : defaultJumpProbability;

        int jumpCooldown = options.JumpCooldownBeats < 0
            ? defaultJumpCooldown
            : options.JumpCooldownBeats;
        double firstPassRatio = IsFinite(options.FirstPassLinearPlaybackRatio)
            ? Math.Clamp(options.FirstPassLinearPlaybackRatio, 0, 1)
            : defaultFirstPassRatio;

        return new BranchDecisionOptions
        {
            InfiniteMode = options.InfiniteMode,
            MinRandomBranchChance = min,
            MaxRandomBranchChance = max,
            RandomBranchChanceDelta = delta,
            JumpProbability = jumpProbability,
            JumpCooldownBeats = jumpCooldown,
            FirstPassLinearPlaybackRatio = firstPassRatio,
            RotateBranches = options.RotateBranches,
            EscapeOptions = options.EscapeOptions.Normalize()
        };
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private sealed record BranchCandidateSelection(RuntimeBranchEdge? Branch, int BlockedCount, string GuardReason);
}
