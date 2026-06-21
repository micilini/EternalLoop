using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class BranchDecisionEngine
{
    private const double ReferenceBeatDurationSeconds = 0.5;

    private readonly BranchDecisionOptions _options;
    private readonly IBranchRandomProvider _randomProvider;
    private readonly BranchEscapeGuard _escapeGuard;
    private readonly Dictionary<RuntimeBeat, int> _lastDestBySource = new(ReferenceEqualityComparer.Instance);
    private RuntimeLinearBeatIndex? _linearBeatIndex;
    private RuntimeBeat? _linearBeatIndexFirstBeat;
    private RuntimeBranchOrder? _branchOrder;
    private RuntimeLinearBeatIndex? _branchOrderLinearBeatIndex;
    private double _branchChance;
    private int _beatsSinceLastJump = int.MaxValue;
    private bool _firstPassActive = true;
    private bool _bringItHome;

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

    public bool BringItHome => _bringItHome;

    public void SetBringItHome(bool enabled) => _bringItHome = enabled;

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

        if (!_options.ContinuousMode)
        {
            return CreateResult(currentBeat, seedBeat, seedBeat, null, chanceBeforeDecision, _branchChance, 1, "Continuous mode disabled");
        }

        if (_bringItHome)
        {
            IncrementBeatsSinceLastJump();
            return CreateResult(
                currentBeat,
                seedBeat,
                seedBeat,
                null,
                chanceBeforeDecision,
                _branchChance,
                1,
                "Bring it home",
                bringItHomeActive: true);
        }

        BranchCandidateSelection selection = SelectSafeBranch(currentBeat, seedBeat, linearBeatIndex, branchOrder);
        RuntimeBranchEdge? branch = selection.Branch;

        if (branch is null)
        {
            IncreaseChance(currentBeat.Duration);
            IncrementBeatsSinceLastJump();
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
                false,
                candidateCountConsidered: selection.SafeCount);
        }

        bool shouldForceEndGuardJump = _options.EscapeOptions.Enabled
            && _options.EscapeOptions.ForceJumpInEndGuard
            && _escapeGuard.IsInEndZone(seedBeat, linearBeatIndex);

        if (shouldForceEndGuardJump)
        {
            ConfirmJump(seedBeat, branch, branchOrder);
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
                true,
                candidateCountConsidered: selection.SafeCount);
        }

        if (IsBlockedByFirstPass(seedBeat, linearBeatIndex))
        {
            IncreaseChance(currentBeat.Duration);
            IncrementBeatsSinceLastJump();
            return CreateResult(
                currentBeat,
                seedBeat,
                seedBeat,
                null,
                chanceBeforeDecision,
                _branchChance,
                1,
                "First pass linear playback",
                false,
                selection.GuardReason,
                selection.BlockedCount,
                false,
                blockedByFirstPass: true,
                candidateCountConsidered: selection.SafeCount);
        }

        if (IsBlockedByCooldown())
        {
            IncreaseChance(currentBeat.Duration);
            IncrementBeatsSinceLastJump();
            return CreateResult(
                currentBeat,
                seedBeat,
                seedBeat,
                null,
                chanceBeforeDecision,
                _branchChance,
                1,
                "Jump cooldown active",
                false,
                selection.GuardReason,
                selection.BlockedCount,
                false,
                blockedByCooldown: true,
                candidateCountConsidered: selection.SafeCount);
        }

        double randomValue = _randomProvider.NextDouble();
        double effectiveChance = EffectiveChance();

        if (effectiveChance > 0 && randomValue <= effectiveChance)
        {
            ConfirmJump(seedBeat, branch, branchOrder);
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
                false,
                candidateCountConsidered: selection.SafeCount);
        }

        IncreaseChance(currentBeat.Duration);
        IncrementBeatsSinceLastJump();
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
            false,
            candidateCountConsidered: selection.SafeCount);
    }

    public void Reset()
    {
        _branchChance = _options.MinRandomBranchChance;
        _beatsSinceLastJump = int.MaxValue;
        _firstPassActive = true;
        _bringItHome = false;
        _lastDestBySource.Clear();
    }

    private BranchCandidateSelection SelectSafeBranch(
        RuntimeBeat currentBeat,
        RuntimeBeat seedBeat,
        RuntimeLinearBeatIndex linearBeatIndex,
        RuntimeBranchOrder branchOrder)
    {
        IReadOnlyList<RuntimeBranchEdge> candidates = branchOrder.GetCandidates(seedBeat);

        if (!_options.WeightedBranchSelection)
        {
            return SelectFirstSafeBranch(currentBeat, seedBeat, linearBeatIndex, candidates);
        }

        List<RuntimeBranchEdge> safe = [];
        int blockedCount = 0;
        string guardReason = string.Empty;

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
                safe.Add(branch);
                continue;
            }

            blockedCount++;
            guardReason = escapeResult.Reason;
        }

        if (safe.Count == 0)
        {
            return new BranchCandidateSelection(null, blockedCount, guardReason, 0);
        }

        if (safe.Count == 1)
        {
            return new BranchCandidateSelection(safe[0], blockedCount, guardReason, 1);
        }

        return new BranchCandidateSelection(SelectWeightedBranch(seedBeat, safe), blockedCount, guardReason, safe.Count);
    }

    private BranchCandidateSelection SelectFirstSafeBranch(
        RuntimeBeat currentBeat,
        RuntimeBeat seedBeat,
        RuntimeLinearBeatIndex linearBeatIndex,
        IReadOnlyList<RuntimeBranchEdge> candidates)
    {
        int blockedCount = 0;
        string guardReason = string.Empty;

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
                return new BranchCandidateSelection(branch, blockedCount, guardReason, 1);
            }

            blockedCount++;
            guardReason = escapeResult.Reason;
        }

        return new BranchCandidateSelection(null, blockedCount, guardReason, 0);
    }

    private RuntimeBranchEdge SelectWeightedBranch(RuntimeBeat seedBeat, List<RuntimeBranchEdge> safe)
    {
        int lastDest = _lastDestBySource.TryGetValue(seedBeat, out int destination)
            ? destination
            : int.MinValue;

        double total = 0;
        Span<double> weights = safe.Count <= 16 ? stackalloc double[safe.Count] : new double[safe.Count];

        for (int index = 0; index < safe.Count; index++)
        {
            RuntimeBranchEdge edge = safe[index];
            double distance = double.IsFinite(edge.Distance) ? Math.Max(0, edge.Distance) : 0;
            double weight = 1.0 / (1.0 + distance);

            if (edge.DestinationBeat.Which == lastDest)
            {
                weight *= _options.RepeatPenalty;
            }

            if (!double.IsFinite(weight) || weight < 0)
            {
                weight = 0;
            }

            weights[index] = weight;
            total += weight;
        }

        if (total <= 0)
        {
            return safe[0];
        }

        double target = _randomProvider.NextDouble() * total;
        for (int index = 0; index < safe.Count; index++)
        {
            target -= weights[index];
            if (target <= 0)
            {
                return safe[index];
            }
        }

        return safe[^1];
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
        if (_options.WeightedBranchSelection || !_options.RotateBranches)
        {
            return;
        }

        branchOrder.MoveToEnd(seedBeat, branch);
    }

    private void ConfirmJump(RuntimeBeat seedBeat, RuntimeBranchEdge branch, RuntimeBranchOrder branchOrder)
    {
        AdvanceBranchOrder(seedBeat, branch, branchOrder);

        if (_options.WeightedBranchSelection)
        {
            _lastDestBySource[seedBeat] = branch.DestinationBeat.Which;
        }

        _beatsSinceLastJump = 0;
        _firstPassActive = false;
        _branchChance = _options.MinRandomBranchChance;
    }

    private void IncrementBeatsSinceLastJump()
    {
        if (_beatsSinceLastJump < int.MaxValue)
        {
            _beatsSinceLastJump++;
        }
    }

    private bool IsBlockedByFirstPass(RuntimeBeat seedBeat, RuntimeLinearBeatIndex linearBeatIndex)
    {
        if (!_options.EnableJumpShapingKnobs
            || !_firstPassActive
            || _options.FirstPassLinearPlaybackRatio <= 0)
        {
            return false;
        }

        int ordinal = linearBeatIndex.GetOrdinalOrWhich(seedBeat);
        int gate = (int)Math.Floor(linearBeatIndex.Count * _options.FirstPassLinearPlaybackRatio);
        return ordinal < gate;
    }

    private bool IsBlockedByCooldown()
    {
        return _options.EnableJumpShapingKnobs
            && _options.JumpCooldownBeats > 0
            && _beatsSinceLastJump < _options.JumpCooldownBeats;
    }

    private double EffectiveChance()
    {
        double chance = _branchChance;
        if (_options.EnableJumpShapingKnobs)
        {
            chance *= _options.JumpProbability;
        }

        return Math.Clamp(chance, 0, 1);
    }

    private void IncreaseChance(double beatDurationSeconds)
    {
        double delta = _options.RandomBranchChanceDelta;
        if (_options.NormalizeChanceDeltaByTempo)
        {
            double duration = double.IsFinite(beatDurationSeconds) && beatDurationSeconds > 0
                ? beatDurationSeconds
                : ReferenceBeatDurationSeconds;
            delta *= duration / ReferenceBeatDurationSeconds;
        }

        _branchChance = Math.Min(_options.MaxRandomBranchChance, _branchChance + delta);
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
        string reason,
        bool bringItHomeActive = false)
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
            false,
            bringItHomeActive: bringItHomeActive);
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
        bool forcedEndGuardJump,
        bool blockedByFirstPass = false,
        bool blockedByCooldown = false,
        bool bringItHomeActive = false,
        int candidateCountConsidered = 0)
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
            ForcedEndGuardJump = forcedEndGuardJump,
            BlockedByFirstPass = blockedByFirstPass,
            BlockedByCooldown = blockedByCooldown,
            BringItHomeActive = bringItHomeActive,
            CandidateCountConsidered = candidateCountConsidered
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
        double repeatPenalty = IsFinite(options.RepeatPenalty)
            && options.RepeatPenalty >= 0
            && options.RepeatPenalty <= 1
                ? options.RepeatPenalty
                : 1.0;

        return new BranchDecisionOptions
        {
            ContinuousMode = options.ContinuousMode,
            MinRandomBranchChance = min,
            MaxRandomBranchChance = max,
            RandomBranchChanceDelta = delta,
            JumpProbability = jumpProbability,
            JumpCooldownBeats = jumpCooldown,
            FirstPassLinearPlaybackRatio = firstPassRatio,
            RotateBranches = options.RotateBranches,
            EnableJumpShapingKnobs = options.EnableJumpShapingKnobs,
            NormalizeChanceDeltaByTempo = options.NormalizeChanceDeltaByTempo,
            WeightedBranchSelection = options.WeightedBranchSelection,
            RepeatPenalty = repeatPenalty,
            EscapeOptions = options.EscapeOptions.Normalize()
        };
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private sealed record BranchCandidateSelection(RuntimeBranchEdge? Branch, int BlockedCount, string GuardReason, int SafeCount);
}
