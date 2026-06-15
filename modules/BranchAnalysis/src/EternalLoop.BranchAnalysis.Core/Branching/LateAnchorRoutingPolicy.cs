using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public static class LateAnchorRoutingPolicy
{
    public const int Unreachable = int.MaxValue;

    private const int BranchTolerance = 1;
    private const double ImmediateBackwardRatio = 0.60;
    private const double EarliestSlackPercent = 0.02;
    private const int EarliestSlackMinimumBeats = 4;
    private const double ExtraHopMaxLandingPercent = 0.50;
    private const int NearbySourceWindowBeats = 4;
    private const double NearbyImmediateRatio = 0.80;

    public static LateAnchorDecisionContext BuildDecisionContext(
        IReadOnlyList<TimeQuantum> quanta,
        int earlyReturnTargetPercent)
    {
        int earlyTarget = ResolveEarlyReturnTargetBeat(quanta, earlyReturnTargetPercent);

        return new LateAnchorDecisionContext
        {
            EarlyReturnTargetBeat = earlyTarget,
            BranchesToEarlyReturnTarget = CalculateBranchesToEarlyReturnTarget(quanta, earlyTarget),
            EarliestReachableByBeat = CalculateEarliestReachableByBeat(quanta)
        };
    }

    public static int ResolveEarlyReturnTargetBeat(
        IReadOnlyList<TimeQuantum> quanta,
        int earlyReturnTargetPercent)
    {
        if (quanta.Count == 0)
        {
            return 0;
        }

        int fallbackBeat = PercentIndex(quanta.Count, earlyReturnTargetPercent);
        int lateHintCapBeat = PercentIndex(quanta.Count, 55);
        int lateSourceStart = PercentIndex(quanta.Count, 66);
        int firstBackwardDestination = Unreachable;
        int firstLateBackwardDestination = Unreachable;

        for (int index = 0; index < quanta.Count; index++)
        {
            TimeQuantum source = quanta[index];

            foreach (BranchEdge edge in GetActiveNeighbors(source))
            {
                if (!IsBackwardEdge(index, edge))
                {
                    continue;
                }

                int destination = edge.Destination!.Which;
                firstBackwardDestination = Math.Min(firstBackwardDestination, destination);

                if (index >= lateSourceStart)
                {
                    firstLateBackwardDestination = Math.Min(firstLateBackwardDestination, destination);
                }
            }
        }

        if (firstBackwardDestination == Unreachable)
        {
            return fallbackBeat;
        }

        int boundedLateHint = firstLateBackwardDestination == Unreachable
            ? firstBackwardDestination
            : Math.Min(firstLateBackwardDestination, lateHintCapBeat);

        return Math.Clamp(
            Math.Max(fallbackBeat, Math.Max(firstBackwardDestination, boundedLateHint)),
            0,
            quanta.Count - 1);
    }

    public static IReadOnlyDictionary<int, int> CalculateBranchesToEarlyReturnTarget(
        IReadOnlyList<TimeQuantum> quanta,
        int earlyTarget)
    {
        Dictionary<int, int> costs = [];

        foreach (TimeQuantum quantum in quanta)
        {
            costs[quantum.Which] = quantum.Which <= earlyTarget ? 0 : Unreachable;
        }

        IterateUntilStable(quanta, quantum =>
        {
            int current = costs[quantum.Which];
            int best = current;

            if (quantum.Next is not null && costs.TryGetValue(quantum.Next.Which, out int nextCost))
            {
                best = Math.Min(best, nextCost);
            }

            foreach (BranchEdge edge in GetActiveNeighbors(quantum))
            {
                if (edge.Destination is not null
                    && costs.TryGetValue(edge.Destination.Which, out int destinationCost)
                    && destinationCost != Unreachable)
                {
                    best = Math.Min(best, destinationCost + 1);
                }
            }

            if (best != current)
            {
                costs[quantum.Which] = best;
                return true;
            }

            return false;
        });

        return costs;
    }

    public static IReadOnlyDictionary<int, int> CalculateEarliestReachableByBeat(
        IReadOnlyList<TimeQuantum> quanta)
    {
        Dictionary<int, int> earliest = [];

        foreach (TimeQuantum quantum in quanta)
        {
            earliest[quantum.Which] = quantum.Which;
        }

        IterateUntilStable(quanta, quantum =>
        {
            int current = earliest[quantum.Which];
            int best = current;

            if (quantum.Next is not null && earliest.TryGetValue(quantum.Next.Which, out int nextEarliest))
            {
                best = Math.Min(best, nextEarliest);
            }

            foreach (BranchEdge edge in GetActiveNeighbors(quantum))
            {
                if (edge.Destination is not null
                    && earliest.TryGetValue(edge.Destination.Which, out int destinationEarliest))
                {
                    best = Math.Min(best, destinationEarliest);
                }
            }

            if (best != current)
            {
                earliest[quantum.Which] = best;
                return true;
            }

            return false;
        });

        return earliest;
    }

    public static IReadOnlyList<LateAnchorTierRule> BuildTierRules(double minLongBranch)
    {
        int minLong = Math.Max(1, (int)Math.Floor(minLongBranch));

        return
        [
            new LateAnchorTierRule
            {
                MaxAdditionalBranches = 1,
                MinImmediateBackwardBeats = minLong
            },
            new LateAnchorTierRule
            {
                MaxAdditionalBranches = 2,
                MinImmediateBackwardBeats = Math.Max(2, (int)Math.Floor(minLong * 0.5))
            },
            new LateAnchorTierRule
            {
                MaxAdditionalBranches = 3,
                MinImmediateBackwardBeats = Math.Max(1, (int)Math.Floor(minLong * 0.25))
            }
        ];
    }

    public static LateAnchorSourceCandidate? FindBestTieredAnchorSource(
        IReadOnlyList<TimeQuantum> quanta,
        LateAnchorDecisionContext context,
        int minSourceIndex,
        double minLongBranch)
    {
        if (quanta.Count == 0)
        {
            return null;
        }

        int preferredLateStart = Math.Max(minSourceIndex, PercentIndex(quanta.Count, 80));
        foreach (LateAnchorTierRule rule in BuildTierRules(minLongBranch))
        {
            LateAnchorSourceCandidate? preferred = FindBestCandidateInRange(
                quanta,
                context,
                rule,
                preferredLateStart,
                quanta.Count - 1);

            if (preferred is not null)
            {
                return preferred;
            }

            if (minSourceIndex < preferredLateStart)
            {
                LateAnchorSourceCandidate? fallback = FindBestCandidateInRange(
                    quanta,
                    context,
                    rule,
                    minSourceIndex,
                    preferredLateStart - 1);

                if (fallback is not null)
                {
                    return fallback;
                }
            }
        }

        return null;
    }

    public static LateAnchorSourceCandidate? SelectExistingAnchorSource(
        IReadOnlyList<TimeQuantum> quanta,
        LateAnchorDecisionContext context,
        int minSourceIndex,
        int fallbackSourceIndex,
        double minLongBranch)
    {
        LateAnchorSourceCandidate? tiered = FindBestTieredAnchorSource(
            quanta,
            context,
            minSourceIndex,
            minLongBranch);

        if (tiered is not null)
        {
            return tiered;
        }

        List<LateAnchorSourceCandidate> direct = [];
        int start = Math.Clamp(fallbackSourceIndex, 0, Math.Max(0, quanta.Count - 1));

        for (int index = start; index < quanta.Count; index++)
        {
            LateAnchorSourceCandidate? candidate = SelectBestBackwardNeighborCost(quanta[index], context);

            if (candidate?.Cost.BranchesToEarlyReturnTarget == 0)
            {
                direct.Add(candidate);
            }
        }

        if (direct.Count == 0)
        {
            return null;
        }

        direct.Sort(CompareExistingAnchorCandidates);
        return direct[0];
    }

    public static LateAnchorRoutingResult InsertBestAnchorBranch(
        IReadOnlyList<TimeQuantum> quanta,
        BranchGraphData data,
        double threshold,
        double maxThreshold,
        int minSourceIndex,
        LateAnchorDecisionContext context)
    {
        List<LateAnchorSourceCandidate> candidates = [];
        int start = Math.Clamp(minSourceIndex, 0, Math.Max(0, quanta.Count - 1));

        for (int index = start; index < quanta.Count; index++)
        {
            TimeQuantum source = quanta[index];

            foreach (BranchEdge edge in source.AllNeighbors ?? [])
            {
                if (edge.Deleted || edge.Destination is null)
                {
                    continue;
                }

                if (source.Neighbors.Contains(edge))
                {
                    continue;
                }

                int immediateBackward = source.Which - edge.Destination.Which;
                double acousticDistance = NearestNeighborCalculator.GetAcousticDistance(edge);

                if (immediateBackward <= 0
                    || acousticDistance >= maxThreshold
                    || acousticDistance <= threshold)
                {
                    continue;
                }

                BranchReturnCost cost = CreateDestinationCost(edge, context, immediateBackward, acousticDistance);

                if (cost.BranchesToEarlyReturnTarget == Unreachable)
                {
                    continue;
                }

                candidates.Add(new LateAnchorSourceCandidate
                {
                    SourceIndex = source.Which,
                    Source = source,
                    Edge = edge,
                    Cost = cost
                });
            }
        }

        if (candidates.Count == 0)
        {
            return new LateAnchorRoutingResult
            {
                Decision = "no-anchor",
                Reason = "no-insertable-anchor",
                EarlyReturnTargetBeat = context.EarlyReturnTargetBeat
            };
        }

        candidates.Sort(CompareInsertCandidates);
        LateAnchorSourceCandidate selected = candidates[0];

        if (!selected.Source.Neighbors.Contains(selected.Edge))
        {
            selected.Source.Neighbors.Add(selected.Edge);
        }

        return CreateResult(selected, selected.Edge, context.EarlyReturnTargetBeat, "inserted-anchor-branch", "inserted");
    }

    public static LateAnchorSourceCandidate? SelectBestBackwardNeighborCost(
        TimeQuantum source,
        LateAnchorDecisionContext context)
    {
        List<LateAnchorSourceCandidate> candidates = [];

        foreach (BranchEdge edge in GetActiveNeighbors(source))
        {
            if (!IsBackwardEdge(source.Which, edge))
            {
                continue;
            }

            int immediateBackward = source.Which - edge.Destination!.Which;
            double acousticDistance = NearestNeighborCalculator.GetAcousticDistance(edge);
            BranchReturnCost cost = CreateDestinationCost(edge, context, immediateBackward, acousticDistance);

            if (cost.BranchesToEarlyReturnTarget == Unreachable)
            {
                continue;
            }

            candidates.Add(new LateAnchorSourceCandidate
            {
                SourceIndex = source.Which,
                Source = source,
                Edge = edge,
                Cost = cost
            });
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        candidates.Sort(CompareBackwardNeighborCandidates);
        return candidates[0];
    }

    public static LateAnchorRoutingResult CreateResult(
        LateAnchorSourceCandidate candidate,
        BranchEdge? insertedEdge,
        int earlyReturnTargetBeat,
        string decision,
        string reason)
    {
        return new LateAnchorRoutingResult
        {
            LastBranchPoint = candidate.SourceIndex,
            InsertedEdge = insertedEdge,
            SelectedAnchorEdge = candidate.Edge,
            Decision = decision,
            Reason = reason,
            EarlyReturnTargetBeat = earlyReturnTargetBeat,
            BranchesToEarlyReturnTarget = candidate.Cost.BranchesToEarlyReturnTarget,
            EarliestReachableBeat = candidate.Cost.EarliestReachable,
            ImmediateBackwardBeats = candidate.Cost.ImmediateBackwardBeats,
            AnchorDistance = candidate.Cost.AcousticDistance
        };
    }

    private static LateAnchorSourceCandidate? FindBestCandidateInRange(
        IReadOnlyList<TimeQuantum> quanta,
        LateAnchorDecisionContext context,
        LateAnchorTierRule rule,
        int start,
        int end)
    {
        List<LateAnchorSourceCandidate> candidates = [];
        int safeStart = Math.Clamp(start, 0, Math.Max(0, quanta.Count - 1));
        int safeEnd = Math.Clamp(end, 0, Math.Max(0, quanta.Count - 1));

        if (safeStart > safeEnd)
        {
            return null;
        }

        for (int index = safeStart; index <= safeEnd; index++)
        {
            LateAnchorSourceCandidate? candidate = SelectBestBackwardNeighborCost(quanta[index], context);

            if (candidate is null
                || candidate.Cost.BranchesToEarlyReturnTarget > rule.MaxAdditionalBranches
                || candidate.Cost.EarliestReachable > context.EarlyReturnTargetBeat
                || candidate.Cost.ImmediateBackwardBeats < rule.MinImmediateBackwardBeats)
            {
                continue;
            }

            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        candidates.Sort(CompareBackwardNeighborCandidates);
        LateAnchorSourceCandidate bestQuality = candidates[0];
        int earliestSlack = Math.Max(EarliestSlackMinimumBeats, (int)Math.Floor(quanta.Count * EarliestSlackPercent));
        int immediateFloor = Math.Max(
            rule.MinImmediateBackwardBeats,
            (int)Math.Floor(bestQuality.Cost.ImmediateBackwardBeats * ImmediateBackwardRatio));
        int extraHopMaxLandingBeat = (int)Math.Floor(quanta.Count * ExtraHopMaxLandingPercent);

        List<LateAnchorSourceCandidate> lateBiasCandidates = candidates
            .Where(candidate => IsLateBiasAcceptable(
                candidate,
                bestQuality,
                immediateFloor,
                earliestSlack,
                extraHopMaxLandingBeat))
            .ToList();

        if (lateBiasCandidates.Count == 0)
        {
            return bestQuality;
        }

        lateBiasCandidates.Sort(CompareLateBiasCandidates);
        LateAnchorSourceCandidate latest = lateBiasCandidates[0];

        if (Math.Abs(latest.SourceIndex - bestQuality.SourceIndex) <= NearbySourceWindowBeats
            && latest.Cost.BranchesToEarlyReturnTarget > bestQuality.Cost.BranchesToEarlyReturnTarget
            && latest.Cost.ImmediateBackwardBeats < bestQuality.Cost.ImmediateBackwardBeats * NearbyImmediateRatio)
        {
            return bestQuality;
        }

        return latest;
    }

    private static bool IsLateBiasAcceptable(
        LateAnchorSourceCandidate candidate,
        LateAnchorSourceCandidate bestQuality,
        int immediateFloor,
        int earliestSlack,
        int extraHopMaxLandingBeat)
    {
        if (candidate.Cost.BranchesToEarlyReturnTarget > bestQuality.Cost.BranchesToEarlyReturnTarget + BranchTolerance
            || candidate.Cost.EarliestReachable > bestQuality.Cost.EarliestReachable + earliestSlack
            || candidate.Cost.ImmediateBackwardBeats < immediateFloor)
        {
            return false;
        }

        bool needsExtraHop = candidate.Cost.BranchesToEarlyReturnTarget > bestQuality.Cost.BranchesToEarlyReturnTarget;
        int landingBeat = candidate.Edge.Destination?.Which ?? Unreachable;
        return !needsExtraHop || landingBeat <= extraHopMaxLandingBeat;
    }

    private static BranchReturnCost CreateDestinationCost(
        BranchEdge edge,
        LateAnchorDecisionContext context,
        int immediateBackward,
        double acousticDistance)
    {
        int destination = edge.Destination?.Which ?? Unreachable;

        return new BranchReturnCost
        {
            BranchesToEarlyReturnTarget = GetMapValue(context.BranchesToEarlyReturnTarget, destination, Unreachable),
            EarliestReachable = GetMapValue(context.EarliestReachableByBeat, destination, Unreachable),
            ImmediateBackwardBeats = immediateBackward,
            AcousticDistance = acousticDistance
        };
    }

    private static void IterateUntilStable(IReadOnlyList<TimeQuantum> quanta, Func<TimeQuantum, bool> update)
    {
        for (int iteration = 0; iteration < PostProcessNearestNeighbors.ReachabilityMaxIterations; iteration++)
        {
            bool changed = false;

            for (int index = quanta.Count - 1; index >= 0; index--)
            {
                changed |= update(quanta[index]);
            }

            if (!changed)
            {
                break;
            }
        }
    }

    private static int CompareBackwardNeighborCandidates(LateAnchorSourceCandidate left, LateAnchorSourceCandidate right)
    {
        int compare = CompareCost(left.Cost, right.Cost);
        return compare != 0
            ? compare
            : right.SourceIndex.CompareTo(left.SourceIndex);
    }

    private static int CompareInsertCandidates(LateAnchorSourceCandidate left, LateAnchorSourceCandidate right)
    {
        int compare = CompareCost(left.Cost, right.Cost);
        return compare != 0
            ? compare
            : right.SourceIndex.CompareTo(left.SourceIndex);
    }

    private static int CompareExistingAnchorCandidates(LateAnchorSourceCandidate left, LateAnchorSourceCandidate right)
    {
        int compare = CompareCost(left.Cost, right.Cost);
        return compare != 0
            ? compare
            : right.SourceIndex.CompareTo(left.SourceIndex);
    }

    private static int CompareLateBiasCandidates(LateAnchorSourceCandidate left, LateAnchorSourceCandidate right)
    {
        int sourceCompare = right.SourceIndex.CompareTo(left.SourceIndex);
        return sourceCompare != 0
            ? sourceCompare
            : CompareCost(left.Cost, right.Cost);
    }

    private static int CompareCost(BranchReturnCost left, BranchReturnCost right)
    {
        int branchCompare = left.BranchesToEarlyReturnTarget.CompareTo(right.BranchesToEarlyReturnTarget);
        if (branchCompare != 0)
        {
            return branchCompare;
        }

        int earliestCompare = left.EarliestReachable.CompareTo(right.EarliestReachable);
        if (earliestCompare != 0)
        {
            return earliestCompare;
        }

        int distanceCompare = left.AcousticDistance.CompareTo(right.AcousticDistance);
        if (distanceCompare != 0)
        {
            return distanceCompare;
        }

        return right.ImmediateBackwardBeats.CompareTo(left.ImmediateBackwardBeats);
    }

    private static bool IsBackwardEdge(int sourceIndex, BranchEdge edge)
    {
        return !edge.Deleted
            && edge.Destination is not null
            && sourceIndex - edge.Destination.Which > 0;
    }

    private static int PercentIndex(int count, int percent)
    {
        int safePercent = Math.Clamp(percent, 0, 100);
        return Math.Clamp((int)Math.Floor(count * safePercent / 100.0), 0, Math.Max(0, count - 1));
    }

    private static int GetMapValue(IReadOnlyDictionary<int, int> values, int key, int fallback)
    {
        return values.TryGetValue(key, out int value) ? value : fallback;
    }

    private static List<BranchEdge> GetActiveNeighbors(TimeQuantum quantum)
    {
        quantum.Neighbors ??= [];
        return quantum.Neighbors;
    }
}
