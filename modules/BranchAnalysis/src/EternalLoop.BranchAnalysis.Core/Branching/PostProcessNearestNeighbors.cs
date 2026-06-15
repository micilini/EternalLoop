using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public static class PostProcessNearestNeighbors
{
    public const string DefaultQuantumType = NearestNeighborCalculator.BeatsType;
    public const double LastEdgeLongBranchPercentThreshold = 50;
    public const double RelaxedLastEdgeMaxThreshold = 65;
    public const double StrictLastEdgeMaxThreshold = 55;
    public const int ReachabilityMaxIterations = 1000;
    public const double ReachThresholdPercent = 50;

    public static BranchGraphData PostProcess(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type = DefaultQuantumType)
    {
        BranchGraphData data = NormalizeBranchGraphData(track, branchGraphData, type);

        RemoveDeletedEdges(data);

        LateAnchorRoutingResult? lateAnchorResult = null;

        if (data.AddLastEdge)
        {
            if (data.LateAnchorRouting)
            {
                lateAnchorResult = RunLateAnchorRouting(track, data, type);

                if (lateAnchorResult.SelectedAnchorEdge is null)
                {
                    InsertLegacyLastEdge(track, data, type);
                    data.LateAnchorDecision = "legacy-fallback";
                    data.LateAnchorReason = "no-anchor";
                }
            }
            else
            {
                InsertLegacyLastEdge(track, data, type);
            }
        }

        CalculateReachability(track, data, type);

        if (data.LateAnchorRouting && lateAnchorResult?.SelectedAnchorEdge is not null && lateAnchorResult.LastBranchPoint >= 0)
        {
            data.LastBranchPoint = lateAnchorResult.LastBranchPoint;
            data.LongestReach = CalculateReachPercent(track, type, data.LastBranchPoint);
            data.LateAnchorDecision = lateAnchorResult.Decision;
            data.LateAnchorReason = lateAnchorResult.Reason;
            data.LateAnchorEarlyReturnTargetBeat = lateAnchorResult.EarlyReturnTargetBeat;
            data.LateAnchorBranchesToTarget = ToExportCost(lateAnchorResult.BranchesToEarlyReturnTarget);
            data.LateAnchorEarliestReachableBeat = ToExportCost(lateAnchorResult.EarliestReachableBeat);
            data.LateAnchorImmediateBackwardBeats = lateAnchorResult.ImmediateBackwardBeats;
            data.LateAnchorDistance = lateAnchorResult.AnchorDistance;
            data.LateAnchorInsertedEdgeId = lateAnchorResult.InsertedEdge?.Id ?? -1;
            data.LateAnchorSelectedEdgeId = lateAnchorResult.SelectedAnchorEdge.Id;
        }
        else
        {
            data.LastBranchPoint = FindBestLastBeat(track, data, type);
        }

        FilterOutBadBranches(track, type, data.LastBranchPoint);

        if (data.RemoveSequentialBranches)
        {
            FilterOutSequentialBranches(track, data, type);
        }

        if (data.AntiLocalLoopPolicy)
        {
            StructuralBranchContext context = data.StructuralContext
                ?? StructuralBranchPolicy.BuildStructuralBranchContext(track, type, data);
            BranchTopologyPolicy.ReduceLocalLoopBranches(track, data, type, context, StructuralBranchOptions.FromBranchGraphData(data));
        }

        data.LocalLoopRiskBranches = BranchTopologyPolicy.CountLocalLoopRiskBranches(track, type);
        data.BranchCount = CountActiveBranches(track, type);

        return data;
    }

    private static void InsertLegacyLastEdge(TrackAnalysisDocument track, BranchGraphData data, string type)
    {
        if (LongestBackwardBranch(track, type) < LastEdgeLongBranchPercentThreshold)
        {
            InsertBestBackwardBranch(track, data, type, data.CurrentThreshold, RelaxedLastEdgeMaxThreshold);
        }
        else
        {
            InsertBestBackwardBranch(track, data, type, data.CurrentThreshold, StrictLastEdgeMaxThreshold);
        }
    }

    private static LateAnchorRoutingResult RunLateAnchorRouting(
        TrackAnalysisDocument track,
        BranchGraphData data,
        string type)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        if (quanta.Count == 0)
        {
            return new LateAnchorRoutingResult
            {
                Decision = "no-anchor",
                Reason = "empty-track"
            };
        }

        double maxAnchorThreshold = LongestBackwardBranch(track, type) < LastEdgeLongBranchPercentThreshold
            ? RelaxedLastEdgeMaxThreshold
            : StrictLastEdgeMaxThreshold;
        LateAnchorDecisionContext context = LateAnchorRoutingPolicy.BuildDecisionContext(
            quanta,
            data.EarlyReturnTargetPercent);
        int preferredStart = PercentIndex(quanta.Count, data.LateAnchorPreferredStartPercent);
        int fallbackStart = PercentIndex(quanta.Count, data.LateAnchorFallbackStartPercent);
        LateAnchorSourceCandidate? existing = LateAnchorRoutingPolicy.SelectExistingAnchorSource(
            quanta,
            context,
            fallbackStart,
            fallbackStart,
            data.MinLongBranch);

        if (existing is not null && existing.SourceIndex >= preferredStart)
        {
            return CreateLateAnchorResult(existing, null, context, "existing-preferred-anchor", "existing");
        }

        LateAnchorRoutingResult insertedPreferred = LateAnchorRoutingPolicy.InsertBestAnchorBranch(
            quanta,
            data,
            data.CurrentThreshold,
            maxAnchorThreshold,
            preferredStart,
            context);

        if (insertedPreferred.SelectedAnchorEdge is not null)
        {
            return CopyDecision(insertedPreferred, context, "inserted-preferred-anchor");
        }

        if (existing is not null)
        {
            return CreateLateAnchorResult(existing, null, context, "existing-fallback-anchor", "existing");
        }

        LateAnchorRoutingResult insertedFallback = LateAnchorRoutingPolicy.InsertBestAnchorBranch(
            quanta,
            data,
            data.CurrentThreshold,
            maxAnchorThreshold,
            fallbackStart,
            context);

        if (insertedFallback.SelectedAnchorEdge is not null)
        {
            return CopyDecision(insertedFallback, context, "inserted-fallback-anchor");
        }

        return new LateAnchorRoutingResult
        {
            Decision = "no-anchor",
            Reason = "no-anchor",
            EarlyReturnTargetBeat = context.EarlyReturnTargetBeat
        };
    }

    private static LateAnchorRoutingResult CreateLateAnchorResult(
        LateAnchorSourceCandidate candidate,
        BranchEdge? insertedEdge,
        LateAnchorDecisionContext context,
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
            EarlyReturnTargetBeat = context.EarlyReturnTargetBeat,
            BranchesToEarlyReturnTarget = candidate.Cost.BranchesToEarlyReturnTarget,
            EarliestReachableBeat = candidate.Cost.EarliestReachable,
            ImmediateBackwardBeats = candidate.Cost.ImmediateBackwardBeats,
            AnchorDistance = candidate.Cost.AcousticDistance
        };
    }

    private static LateAnchorRoutingResult CopyDecision(
        LateAnchorRoutingResult result,
        LateAnchorDecisionContext context,
        string decision)
    {
        return new LateAnchorRoutingResult
        {
            LastBranchPoint = result.LastBranchPoint,
            LongestReach = result.LongestReach,
            InsertedEdge = result.InsertedEdge,
            SelectedAnchorEdge = result.SelectedAnchorEdge,
            Decision = decision,
            Reason = result.Reason,
            EarlyReturnTargetBeat = context.EarlyReturnTargetBeat,
            BranchesToEarlyReturnTarget = result.BranchesToEarlyReturnTarget,
            EarliestReachableBeat = result.EarliestReachableBeat,
            ImmediateBackwardBeats = result.ImmediateBackwardBeats,
            AnchorDistance = result.AnchorDistance
        };
    }

    public static void RemoveDeletedEdges(BranchGraphData branchGraphData)
    {
        branchGraphData.DeletedEdges ??= [];

        foreach (BranchEdge edge in branchGraphData.DeletedEdges.ToArray())
        {
            DeleteEdge(edge, branchGraphData);
        }

        branchGraphData.DeletedEdges = [];
    }

    public static void DeleteEdge(BranchEdge edge, BranchGraphData branchGraphData)
    {
        if (edge.Deleted)
        {
            return;
        }

        branchGraphData.DeletedEdgeCount++;
        edge.Deleted = true;
        edge.Source?.Neighbors.Remove(edge);
    }

    public static double LongestBackwardBranch(
        TrackAnalysisDocument track,
        string type = DefaultQuantumType)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        if (quanta.Count == 0)
        {
            return 0;
        }

        int longest = 0;

        for (int index = 0; index < quanta.Count; index++)
        {
            TimeQuantum q = quanta[index];

            foreach (BranchEdge neighbor in GetNeighbors(q))
            {
                if (neighbor.Destination is null)
                {
                    continue;
                }

                int delta = index - neighbor.Destination.Which;

                if (delta > longest)
                {
                    longest = delta;
                }
            }
        }

        return longest * 100.0 / quanta.Count;
    }

    public static BranchEdge? InsertBestBackwardBranch(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type,
        double threshold,
        double maxThreshold)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        List<BackwardBranchCandidate> branches = [];

        for (int index = 0; index < quanta.Count; index++)
        {
            TimeQuantum q = quanta[index];

            foreach (BranchEdge neighbor in GetAllNeighbors(q))
            {
                if (neighbor.Deleted || neighbor.Destination is null)
                {
                    continue;
                }

                int delta = index - neighbor.Destination.Which;

                if (delta > 0 && NearestNeighborCalculator.GetAcousticDistance(neighbor) < maxThreshold)
                {
                    branches.Add(new BackwardBranchCandidate(delta * 100.0 / quanta.Count, q, neighbor));
                }
            }
        }

        if (branches.Count == 0)
        {
            return null;
        }

        branches.Sort(CompareBackwardBranchCandidates);
        BackwardBranchCandidate best = branches[0];

        if (NearestNeighborCalculator.GetAcousticDistance(best.Edge) > threshold
            && !best.Source.Neighbors.Contains(best.Edge))
        {
            best.Source.Neighbors.Add(best.Edge);
        }

        return best.Edge;
    }

    public static void CalculateReachability(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type = DefaultQuantumType)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        foreach (TimeQuantum q in quanta)
        {
            q.Reach = quanta.Count - q.Which;
        }

        for (int iteration = 0; iteration < ReachabilityMaxIterations; iteration++)
        {
            int changeCount = 0;

            for (int index = 0; index < quanta.Count; index++)
            {
                TimeQuantum q = quanta[index];
                bool changed = false;

                foreach (BranchEdge neighbor in GetNeighbors(q))
                {
                    TimeQuantum? destination = neighbor.Destination;

                    if (destination is not null && destination.Reach > q.Reach)
                    {
                        q.Reach = destination.Reach;
                        changed = true;
                    }
                }

                if (index < quanta.Count - 1)
                {
                    TimeQuantum next = quanta[index + 1];

                    if (next.Reach > q.Reach)
                    {
                        q.Reach = next.Reach;
                        changed = true;
                    }
                }

                if (changed)
                {
                    changeCount++;

                    for (int previousIndex = 0; previousIndex < q.Which; previousIndex++)
                    {
                        TimeQuantum previous = quanta[previousIndex];

                        if (previous.Reach < q.Reach)
                        {
                            previous.Reach = q.Reach;
                        }
                    }
                }
            }

            if (changeCount == 0)
            {
                break;
            }
        }

        branchGraphData.TotalBeats = quanta.Count;
    }

    public static int FindBestLastBeat(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type = DefaultQuantumType)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        int longest = 0;
        double longestReach = 0;

        for (int index = quanta.Count - 1; index >= 0; index--)
        {
            TimeQuantum q = quanta[index];
            int distanceToEnd = quanta.Count - index;
            double reach = (q.Reach - distanceToEnd) * 100.0 / quanta.Count;

            if (reach > longestReach && GetNeighbors(q).Count > 0)
            {
                longestReach = reach;
                longest = index;

                if (reach >= ReachThresholdPercent)
                {
                    break;
                }
            }
        }

        branchGraphData.TotalBeats = quanta.Count;
        branchGraphData.LongestReach = longestReach;

        return longest;
    }

    public static void FilterOutBadBranches(TrackAnalysisDocument track, string type, int lastIndex)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        for (int index = 0; index < lastIndex; index++)
        {
            TimeQuantum q = quanta[index];
            q.Neighbors = GetNeighbors(q)
                .Where(neighbor => neighbor.Destination is not null && neighbor.Destination.Which < lastIndex)
                .ToList();
        }
    }

    public static bool HasSequentialBranch(TimeQuantum q, BranchEdge neighbor, BranchGraphData branchGraphData)
    {
        if (q.Which == branchGraphData.LastBranchPoint)
        {
            return false;
        }

        TimeQuantum? previous = q.Prev;

        if (previous is null)
        {
            return false;
        }

        if (neighbor.Destination is null)
        {
            return false;
        }

        int distance = q.Which - neighbor.Destination.Which;

        foreach (BranchEdge previousNeighbor in GetNeighbors(previous))
        {
            if (previousNeighbor.Destination is null)
            {
                continue;
            }

            int previousDistance = previous.Which - previousNeighbor.Destination.Which;

            if (distance == previousDistance)
            {
                return true;
            }
        }

        return false;
    }

    public static void FilterOutSequentialBranches(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type = DefaultQuantumType)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        for (int index = quanta.Count - 1; index >= 1; index--)
        {
            TimeQuantum q = quanta[index];
            q.Neighbors = GetNeighbors(q)
                .Where(neighbor => !HasSequentialBranch(q, neighbor, branchGraphData))
                .ToList();
        }
    }

    public static int CountActiveBranches(
        TrackAnalysisDocument track,
        string type = DefaultQuantumType)
    {
        return GetQuanta(track, type).Sum(q => GetNeighbors(q).Count);
    }

    private static BranchGraphData NormalizeBranchGraphData(TrackAnalysisDocument track, BranchGraphData branchGraphData, string type)
    {
        if (branchGraphData is null)
        {
            throw new PostProcessException("BranchGraphData must be an object.");
        }

        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        branchGraphData.AllEdges ??= [];
        branchGraphData.DeletedEdges ??= [];
        branchGraphData.AddLastEdge = branchGraphData.AddLastEdge;
        branchGraphData.CurrentThreshold = double.IsFinite(branchGraphData.CurrentThreshold) ? branchGraphData.CurrentThreshold : 0;
        branchGraphData.MaxBranchThreshold = double.IsFinite(branchGraphData.MaxBranchThreshold)
            ? branchGraphData.MaxBranchThreshold
            : NearestNeighborCalculator.DefaultMaxBranchThreshold;
        branchGraphData.DeletedEdgeCount = ToNonNegativeInteger(branchGraphData.DeletedEdgeCount, 0);
        branchGraphData.LastBranchPoint = ToNonNegativeInteger(branchGraphData.LastBranchPoint, 0);
        branchGraphData.LongestReach = ToNonNegativeNumber(branchGraphData.LongestReach, 0);
        branchGraphData.TotalBeats = quanta.Count;
        branchGraphData.BranchCount = ToNonNegativeInteger(branchGraphData.BranchCount, 0);
        branchGraphData.AntiMRemovedBranches = ToNonNegativeInteger(branchGraphData.AntiMRemovedBranches, 0);
        branchGraphData.StructurallyRejectedBranches = ToNonNegativeInteger(branchGraphData.StructurallyRejectedBranches, 0);
        branchGraphData.LocalLoopRiskBranches = ToNonNegativeInteger(branchGraphData.LocalLoopRiskBranches, 0);
        branchGraphData.EarlyReturnTargetPercent = ToPercent(
            branchGraphData.EarlyReturnTargetPercent,
            BranchAnalysisDefaults.EarlyReturnTargetPercent);
        branchGraphData.LateAnchorPreferredStartPercent = ToPercent(
            branchGraphData.LateAnchorPreferredStartPercent,
            BranchAnalysisDefaults.LateAnchorPreferredStartPercent);
        branchGraphData.LateAnchorFallbackStartPercent = ToPercent(
            branchGraphData.LateAnchorFallbackStartPercent,
            BranchAnalysisDefaults.LateAnchorFallbackStartPercent);
        branchGraphData.LateAnchorDecision = string.IsNullOrWhiteSpace(branchGraphData.LateAnchorDecision)
            ? "none"
            : branchGraphData.LateAnchorDecision;
        branchGraphData.LateAnchorReason = string.IsNullOrWhiteSpace(branchGraphData.LateAnchorReason)
            ? "not-run"
            : branchGraphData.LateAnchorReason;

        foreach (TimeQuantum q in quanta)
        {
            q.Neighbors ??= [];
            q.AllNeighbors ??= [];
        }

        return branchGraphData;
    }

    private static IReadOnlyList<TimeQuantum> GetQuanta(TrackAnalysisDocument track, string type)
    {
        if (track is null)
        {
            throw new PostProcessException("Track must be an object.");
        }

        if (track.Analysis is null)
        {
            throw new PostProcessException("Track analysis must be an object.");
        }

        return type switch
        {
            NearestNeighborCalculator.SectionsType => track.Analysis.Sections,
            NearestNeighborCalculator.BarsType => track.Analysis.Bars,
            NearestNeighborCalculator.BeatsType => track.Analysis.Beats,
            NearestNeighborCalculator.TatumsType => track.Analysis.Tatums,
            NearestNeighborCalculator.SegmentsType => track.Analysis.Segments,
            NearestNeighborCalculator.FilteredSegmentsType => track.Analysis.FilteredSegments,
            _ => throw new PostProcessException($"Track analysis.{type} must be an array.")
        };
    }

    private static List<BranchEdge> GetNeighbors(TimeQuantum q)
    {
        q.Neighbors ??= [];
        return q.Neighbors;
    }

    private static List<BranchEdge> GetAllNeighbors(TimeQuantum q)
    {
        q.AllNeighbors ??= [];
        return q.AllNeighbors;
    }

    private static int CompareBackwardBranchCandidates(BackwardBranchCandidate left, BackwardBranchCandidate right)
    {
        if (left.Percent != right.Percent)
        {
            return right.Percent.CompareTo(left.Percent);
        }

        double leftAcousticDistance = NearestNeighborCalculator.GetAcousticDistance(left.Edge);
        double rightAcousticDistance = NearestNeighborCalculator.GetAcousticDistance(right.Edge);

        if (leftAcousticDistance != rightAcousticDistance)
        {
            return leftAcousticDistance.CompareTo(rightAcousticDistance);
        }

        double leftScore = GetBranchScore(left.Edge);
        double rightScore = GetBranchScore(right.Edge);

        if (leftScore != rightScore)
        {
            return leftScore.CompareTo(rightScore);
        }

        return (left.Edge.Destination?.Which ?? int.MaxValue).CompareTo(right.Edge.Destination?.Which ?? int.MaxValue);
    }

    private static double GetBranchScore(BranchEdge edge)
    {
        if (double.IsFinite(edge.BranchScore))
        {
            return edge.BranchScore;
        }

        return double.IsFinite(edge.Distance) ? edge.Distance : double.PositiveInfinity;
    }

    private static int ToNonNegativeInteger(int value, int fallback)
    {
        return value >= 0 ? value : fallback;
    }

    private static double ToNonNegativeNumber(double value, double fallback)
    {
        return value >= 0 && double.IsFinite(value) ? value : fallback;
    }

    private static int ToPercent(int value, int fallback)
    {
        return value is >= 0 and <= 100 ? value : fallback;
    }

    private static int ToExportCost(int value)
    {
        return value == LateAnchorRoutingPolicy.Unreachable ? -1 : value;
    }

    private static int PercentIndex(int count, int percent)
    {
        return Math.Clamp((int)Math.Floor(count * Math.Clamp(percent, 0, 100) / 100.0), 0, Math.Max(0, count - 1));
    }

    private static double CalculateReachPercent(TrackAnalysisDocument track, string type, int index)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        if (index < 0 || index >= quanta.Count || quanta.Count == 0)
        {
            return 0;
        }

        TimeQuantum q = quanta[index];
        int distanceToEnd = quanta.Count - index;
        return Math.Max(0, (q.Reach - distanceToEnd) * 100.0 / quanta.Count);
    }

    private sealed record BackwardBranchCandidate(double Percent, TimeQuantum Source, BranchEdge Edge);
}

