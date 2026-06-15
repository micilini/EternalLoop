using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class BranchEscapeGuard
{
    private readonly BranchEscapeOptions _options;

    public BranchEscapeGuard(BranchEscapeOptions? options = null)
    {
        _options = (options ?? new BranchEscapeOptions()).Normalize();
    }

    public BranchEscapeResult Evaluate(
        RuntimeBeat currentBeat,
        RuntimeBeat seedBeat,
        RuntimeBranchEdge branch,
        RuntimeBeat firstBeat)
    {
        ArgumentNullException.ThrowIfNull(currentBeat);
        ArgumentNullException.ThrowIfNull(seedBeat);
        ArgumentNullException.ThrowIfNull(branch);
        ArgumentNullException.ThrowIfNull(firstBeat);

        return Evaluate(currentBeat, seedBeat, branch, RuntimeLinearBeatIndex.FromFirstBeat(firstBeat));
    }

    public BranchEscapeResult Evaluate(
        RuntimeBeat currentBeat,
        RuntimeBeat seedBeat,
        RuntimeBranchEdge branch,
        RuntimeLinearBeatIndex linearBeatIndex)
    {
        ArgumentNullException.ThrowIfNull(currentBeat);
        ArgumentNullException.ThrowIfNull(seedBeat);
        ArgumentNullException.ThrowIfNull(branch);
        ArgumentNullException.ThrowIfNull(linearBeatIndex);

        if (!_options.Enabled)
        {
            return CreateResult(true, "Guard disabled", branch.DestinationBeat?.Which ?? -1, false, true);
        }

        if (linearBeatIndex.Count == 0)
        {
            return CreateResult(false, "No linear beats", branch.DestinationBeat?.Which ?? -1, false, false);
        }

        if (!IsValidBranch(currentBeat, seedBeat, branch, linearBeatIndex, out RuntimeBeat? destination))
        {
            return CreateResult(false, "Invalid branch", branch.DestinationBeat?.Which ?? -1, false, false);
        }

        RuntimeBeat destinationBeat = destination!;
        bool destinationIsInEndZone = IsInEndZone(destinationBeat, linearBeatIndex);

        if (!destinationIsInEndZone)
        {
            return CreateResult(true, "Destination outside end zone", destinationBeat.Which, false, true);
        }

        bool hasFutureEscape = HasFutureEscape(destinationBeat, linearBeatIndex, 0, []);

        return hasFutureEscape
            ? CreateResult(true, "Destination has future escape", destinationBeat.Which, true, true)
            : CreateResult(false, "Destination has no terminal escape", destinationBeat.Which, true, false);
    }

    public bool IsInEndZone(RuntimeBeat beat, RuntimeBeat firstBeat)
    {
        return IsInEndZone(beat, RuntimeLinearBeatIndex.FromFirstBeat(firstBeat));
    }

    public bool IsInEndZone(RuntimeBeat beat, RuntimeLinearBeatIndex linearBeatIndex)
    {
        ArgumentNullException.ThrowIfNull(beat);
        ArgumentNullException.ThrowIfNull(linearBeatIndex);

        if (linearBeatIndex.Count == 0)
        {
            return true;
        }

        int beatIndex = linearBeatIndex.GetOrdinalOrWhich(beat);
        int endGuardStartIndex = (int)Math.Floor(linearBeatIndex.Count * _options.EndGuardStartRatio);
        int beatsBeforeEnd = linearBeatIndex.Count - beatIndex;

        return beatIndex >= endGuardStartIndex
            || beatsBeforeEnd <= _options.MinimumBeatsBeforeEndForJumpDestination;
    }

    private bool HasFutureEscape(
        RuntimeBeat destination,
        RuntimeLinearBeatIndex linearBeatIndex,
        int depth,
        HashSet<RuntimeBeat> visited)
    {
        if (depth >= _options.MaxEscapeSearchDepth || !visited.Add(destination))
        {
            return false;
        }

        RuntimeBeat? beat = destination;

        for (int index = 0; index < _options.TerminalEscapeLookaheadBeats && beat is not null; index++)
        {
            foreach (RuntimeBranchEdge branch in beat.Neighbors)
            {
                if (!IsValidFutureBranch(beat, branch, linearBeatIndex, out RuntimeBeat? branchDestination))
                {
                    continue;
                }

                RuntimeBeat branchDestinationBeat = branchDestination!;

                if (!IsInEndZone(branchDestinationBeat, linearBeatIndex))
                {
                    return true;
                }

                if (HasFutureEscape(branchDestinationBeat, linearBeatIndex, depth + 1, visited))
                {
                    return true;
                }
            }

            beat = beat.Next;
        }

        return false;
    }

    private static bool IsValidBranch(
        RuntimeBeat currentBeat,
        RuntimeBeat seedBeat,
        RuntimeBranchEdge branch,
        RuntimeLinearBeatIndex linearBeatIndex,
        out RuntimeBeat? destination)
    {
        destination = branch.DestinationBeat;

        return !branch.Deleted
            && destination is not null
            && !ReferenceEquals(destination, seedBeat)
            && !ReferenceEquals(destination, currentBeat)
            && linearBeatIndex.Contains(destination);
    }

    private static bool IsValidFutureBranch(
        RuntimeBeat sourceBeat,
        RuntimeBranchEdge branch,
        RuntimeLinearBeatIndex linearBeatIndex,
        out RuntimeBeat? destination)
    {
        destination = branch.DestinationBeat;

        return !branch.Deleted
            && destination is not null
            && !ReferenceEquals(destination, sourceBeat)
            && linearBeatIndex.Contains(destination);
    }

    private static BranchEscapeResult CreateResult(
        bool isSafe,
        string reason,
        int destinationBeatIndex,
        bool destinationIsInEndZone,
        bool hasFutureEscape)
    {
        return new BranchEscapeResult
        {
            IsSafe = isSafe,
            Reason = reason,
            DestinationBeatIndex = destinationBeatIndex,
            DestinationIsInEndZone = destinationIsInEndZone,
            HasFutureEscape = hasFutureEscape
        };
    }
}
