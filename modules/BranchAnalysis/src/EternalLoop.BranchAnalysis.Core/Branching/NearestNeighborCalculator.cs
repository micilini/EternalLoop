using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Distance;
using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public static class NearestNeighborCalculator
{
    public const int DefaultMaxBranches = 4;
    public const double DefaultMaxBranchThreshold = 80;
    public const double DynamicThresholdStart = 10;
    public const double DynamicThresholdStep = 5;
    public const double TargetBranchDivisor = 6;
    public const double SameSegmentPenalty = 100;
    public const double ParentPositionPenalty = 100;
    public const double LongBranchDivisor = 5;
    public const string SectionsType = "sections";
    public const string BarsType = "bars";
    public const string BeatsType = "beats";
    public const string TatumsType = "tatums";
    public const string SegmentsType = "segments";
    public const string FilteredSegmentsType = "fsegments";

    private const int DefaultEstimatedBeatsPerBar = 4;

    public static BranchGraphData CreateBranchGraphData(
        TrackAnalysisDocument track,
        string type = BeatsType,
        NearestNeighborOptions? options = null)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        double maxBranchThreshold = GetPositiveNumber(options?.MaxBranchThreshold, DefaultMaxBranchThreshold);

        return new BranchGraphData
        {
            AllEdges = [],
            SimilarityThreshold = GetFiniteNumber(
                options?.SimilarityThreshold,
                BranchAnalysisDefaults.SimilarityThreshold),
            LookaheadDepth = GetPositiveInteger(
                options?.LookaheadDepth,
                BranchAnalysisDefaults.LookaheadDepth),
            MinJumpDistance = GetPositiveInteger(
                options?.MinJumpDistance,
                BranchAnalysisDefaults.MinJumpDistance),
            MaxBranches = GetPositiveInteger(options?.MaxBranches, DefaultMaxBranches),
            MaxBranchThreshold = maxBranchThreshold,
            CurrentThreshold = GetPositiveNumber(options?.CurrentThreshold, maxBranchThreshold),
            ComputedThreshold = GetPositiveNumber(options?.ComputedThreshold, maxBranchThreshold),
            JustBackwards = options?.JustBackwards == true,
            JustLongBranches = options?.JustLongBranches == true,
            MinLongBranch = GetPositiveNumber(options?.MinLongBranch, quanta.Count / LongBranchDivisor),
            AddLastEdge = options?.AddLastEdge != false,
            RemoveSequentialBranches = options?.RemoveSequentialBranches == true,
            StructuralPolicy = options?.StructuralPolicy ?? BranchAnalysisDefaults.StructuralPolicy,
            AntiLocalLoopPolicy = options?.AntiLocalLoopPolicy ?? BranchAnalysisDefaults.AntiLocalLoopPolicy,
            ShortBranchPolicy = string.IsNullOrWhiteSpace(options?.ShortBranchPolicy)
                ? BranchAnalysisDefaults.ShortBranchPolicy
                : options.ShortBranchPolicy,
            VeryShortBars = GetPositiveInteger(options?.VeryShortBars, BranchAnalysisDefaults.VeryShortBars),
            ShortBars = GetPositiveInteger(options?.ShortBars, BranchAnalysisDefaults.ShortBars),
            PhraseBars = GetPositiveInteger(options?.PhraseBars, BranchAnalysisDefaults.PhraseBars),
            LocalWindowBars = GetPositiveInteger(options?.LocalWindowBars, BranchAnalysisDefaults.LocalWindowBars),
            MaxShortLocalBranchesPerCluster = GetPositiveInteger(
                options?.MaxShortLocalBranchesPerCluster,
                BranchAnalysisDefaults.MaxShortLocalBranchesPerCluster),
            StructurallyRejectedBranches = 0,
            AntiMRemovedBranches = 0,
            LocalLoopRiskBranches = 0,
            DeletedEdges = [],
            DeletedEdgeCount = 0,
            BranchCount = 0,
            LastBranchPoint = 0,
            LongestReach = 0,
            TotalBeats = quanta.Count
        };
    }

    public static int DynamicCalculateNearestNeighbors(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type = BeatsType)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        BranchGraphData data = NormalizeBranchGraphData(track, branchGraphData, type);

        if (quanta.Count == 0)
        {
            data.CurrentThreshold = 0;
            data.ComputedThreshold = 0;
            data.AllEdges = [];
            return 0;
        }

        double targetBranchCount = quanta.Count / TargetBranchDivisor;
        int branchingCount = 0;
        double threshold = DynamicThresholdStart;

        PrecalculateNearestNeighbors(track, data, type, data.MaxBranches, data.MaxBranchThreshold);

        for (; threshold < data.MaxBranchThreshold; threshold += DynamicThresholdStep)
        {
            branchingCount = CollectNearestNeighbors(track, data, type, threshold);

            if (branchingCount >= targetBranchCount)
            {
                break;
            }
        }

        data.CurrentThreshold = threshold;
        data.ComputedThreshold = threshold;

        return branchingCount;
    }

    public static void PrecalculateNearestNeighbors(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type,
        int maxNeighbors,
        double maxThreshold)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        BranchGraphData data = NormalizeBranchGraphData(track, branchGraphData, type);

        data.AllEdges = [];
        data.StructurallyRejectedBranches = 0;
        data.StructuralContext = StructuralBranchPolicy.BuildStructuralBranchContext(track, type, data);

        foreach (TimeQuantum quantum in quanta)
        {
            quantum.AllNeighbors = [];
        }

        if (quanta.Count == 0)
        {
            return;
        }

        foreach (TimeQuantum q1 in quanta)
        {
            CalculateNearestNeighborsForQuantum(track, data, type, maxNeighbors, maxThreshold, q1);
        }
    }

    public static void CalculateNearestNeighborsForQuantum(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type,
        int maxNeighbors,
        double maxThreshold,
        TimeQuantum q1)
    {
        if (q1 is null)
        {
            throw new NearestNeighborException("Quantum must be an object.");
        }

        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        BranchGraphData data = NormalizeBranchGraphData(track, branchGraphData, type);
        List<BranchEdge> edges = [];
        IReadOnlyList<SegmentQuantum> overlappingSegments = q1.OverlappingSegments ?? [];
        q1.AllNeighbors = [];

        if (overlappingSegments.Count == 0)
        {
            return;
        }

        StructuralBranchContext structuralContext = data.StructuralContext
            ?? StructuralBranchPolicy.BuildStructuralBranchContext(track, type, data);
        data.StructuralContext = structuralContext;

        foreach (TimeQuantum q2 in quanta)
        {
            if (q2.Which == q1.Which)
            {
                continue;
            }

            if (data.MinJumpDistance > BranchAnalysisDefaults.MinJumpDistance
                && Math.Abs(q1.Which - q2.Which) < data.MinJumpDistance)
            {
                continue;
            }

            double totalDistance = CalculateQuantumDistance(q1, q2);
            StructuralBranchScore score = StructuralBranchPolicy.ScoreBranchCandidate(
                q1,
                q2,
                totalDistance,
                structuralContext);

            if (!StructuralBranchPolicy.IsStructurallyAllowedBranch(q1, q2, score, structuralContext))
            {
                data.StructurallyRejectedBranches++;
                structuralContext.StructurallyRejectedBranches++;
                continue;
            }

            if (!PassesLookaheadGate(quanta, q1, q2, data.LookaheadDepth, maxThreshold))
            {
                continue;
            }

            if (totalDistance < maxThreshold)
            {
                BranchEdge edge = CreateEdge(edges.Count, q1, q2, totalDistance);
                StructuralBranchPolicy.AttachScoreToEdge(edge, score);
                edges.Add(edge);
            }
        }

        edges.Sort(CompareEdgesByAcousticQuality);
        int limit = Math.Min(maxNeighbors, edges.Count);

        for (int index = 0; index < limit; index++)
        {
            BranchEdge edge = edges[index];
            edge.Id = data.AllEdges.Count;
            q1.AllNeighbors.Add(edge);
            data.AllEdges.Add(edge);
        }
    }

    public static int CollectNearestNeighbors(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type,
        double maxThreshold)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        BranchGraphData data = NormalizeBranchGraphData(track, branchGraphData, type);
        int branchingCount = 0;

        foreach (TimeQuantum q1 in quanta)
        {
            q1.Neighbors = ExtractNearestNeighbors(data, q1, maxThreshold);

            if (q1.Neighbors.Count > 0)
            {
                branchingCount++;
            }
        }

        return branchingCount;
    }

    public static List<BranchEdge> ExtractNearestNeighbors(
        BranchGraphData branchGraphData,
        TimeQuantum q,
        double maxThreshold)
    {
        if (branchGraphData is null)
        {
            throw new NearestNeighborException("BranchGraphData must be an object.");
        }

        if (q is null)
        {
            throw new NearestNeighborException("Quantum must be an object.");
        }

        List<BranchEdge> neighbors = [];

        foreach (BranchEdge neighbor in q.AllNeighbors)
        {
            if (neighbor.Deleted)
            {
                continue;
            }

            if (neighbor.Destination is null)
            {
                continue;
            }

            if (branchGraphData.JustBackwards && neighbor.Destination.Which > q.Which)
            {
                continue;
            }

            if (branchGraphData.JustLongBranches
                && Math.Abs(neighbor.Destination.Which - q.Which) < branchGraphData.MinLongBranch)
            {
                continue;
            }

            if (GetAcousticDistance(neighbor) <= maxThreshold)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public static double CalculateQuantumDistance(TimeQuantum q1, TimeQuantum q2)
    {
        IReadOnlyList<SegmentQuantum> sourceSegments = q1.OverlappingSegments ?? [];
        IReadOnlyList<SegmentQuantum> destinationSegments = q2.OverlappingSegments ?? [];

        if (sourceSegments.Count == 0)
        {
            return double.PositiveInfinity;
        }

        double sum = 0;

        for (int index = 0; index < sourceSegments.Count; index++)
        {
            SegmentQuantum sourceSegment = sourceSegments[index];
            double distance = SameSegmentPenalty;

            if (index < destinationSegments.Count)
            {
                SegmentQuantum destinationSegment = destinationSegments[index];

                if (sourceSegment.Which == destinationSegment.Which)
                {
                    distance = SameSegmentPenalty;
                }
                else
                {
                    distance = SegmentDistanceCalculator.GetSegmentDistances(sourceSegment, destinationSegment);
                }
            }

            sum += distance;
        }

        double parentDistance = q1.IndexInParent == q2.IndexInParent ? 0 : ParentPositionPenalty;

        return sum / sourceSegments.Count + parentDistance;
    }

    public static int CompareEdgesByBranchScore(BranchEdge left, BranchEdge right)
    {
        double leftScore = GetBranchScore(left);
        double rightScore = GetBranchScore(right);

        if (leftScore != rightScore)
        {
            return leftScore.CompareTo(rightScore);
        }

        double leftAcoustic = GetAcousticDistance(left);
        double rightAcoustic = GetAcousticDistance(right);

        if (leftAcoustic != rightAcoustic)
        {
            return leftAcoustic.CompareTo(rightAcoustic);
        }

        double leftJump = GetFiniteNumber(left.JumpBeatsAbs, GetJumpFallback(left));
        double rightJump = GetFiniteNumber(right.JumpBeatsAbs, GetJumpFallback(right));

        if (leftJump != rightJump)
        {
            return rightJump.CompareTo(leftJump);
        }

        return GetDestinationWhich(left).CompareTo(GetDestinationWhich(right));
    }

    public static int CompareEdgesByAcousticQuality(BranchEdge left, BranchEdge right)
    {
        double leftAcousticDistance = GetAcousticDistance(left);
        double rightAcousticDistance = GetAcousticDistance(right);

        if (leftAcousticDistance != rightAcousticDistance)
        {
            return leftAcousticDistance.CompareTo(rightAcousticDistance);
        }

        double leftPenalty = GetFiniteNumber(left.StructuralPenalty, 0);
        double rightPenalty = GetFiniteNumber(right.StructuralPenalty, 0);

        if (leftPenalty != rightPenalty)
        {
            return leftPenalty.CompareTo(rightPenalty);
        }

        if (left.LocalLoopRisk != right.LocalLoopRisk)
        {
            return left.LocalLoopRisk ? 1 : -1;
        }

        if (left.ShortLocalRisk != right.ShortLocalRisk)
        {
            return left.ShortLocalRisk ? 1 : -1;
        }

        double leftJump = GetFiniteNumber(left.JumpBeatsAbs, GetJumpFallback(left));
        double rightJump = GetFiniteNumber(right.JumpBeatsAbs, GetJumpFallback(right));

        if (leftJump != rightJump)
        {
            return rightJump.CompareTo(leftJump);
        }

        return GetDestinationWhich(left).CompareTo(GetDestinationWhich(right));
    }

    public static double GetAcousticDistance(BranchEdge edge)
    {
        if (double.IsFinite(edge.AcousticDistance))
        {
            return edge.AcousticDistance;
        }

        return double.IsFinite(edge.Distance) ? edge.Distance : double.PositiveInfinity;
    }

    private static BranchEdge CreateEdge(
        int id,
        TimeQuantum source,
        TimeQuantum destination,
        double distance)
    {
        return new BranchEdge
        {
            Id = id,
            Source = source,
            Destination = destination,
            Distance = distance,
            Deleted = false,
            Curve = null
        };
    }

    private static BranchGraphData NormalizeBranchGraphData(TrackAnalysisDocument track, BranchGraphData branchGraphData, string type)
    {
        if (branchGraphData is null)
        {
            throw new NearestNeighborException("BranchGraphData must be an object.");
        }

        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);

        branchGraphData.AllEdges ??= [];
        branchGraphData.DeletedEdges ??= [];
        branchGraphData.SimilarityThreshold = GetFiniteNumber(
            branchGraphData.SimilarityThreshold,
            BranchAnalysisDefaults.SimilarityThreshold);
        branchGraphData.LookaheadDepth = GetPositiveInteger(
            branchGraphData.LookaheadDepth,
            BranchAnalysisDefaults.LookaheadDepth);
        branchGraphData.MinJumpDistance = GetPositiveInteger(
            branchGraphData.MinJumpDistance,
            BranchAnalysisDefaults.MinJumpDistance);
        branchGraphData.MaxBranches = GetPositiveInteger(branchGraphData.MaxBranches, DefaultMaxBranches);
        branchGraphData.MaxBranchThreshold = GetPositiveNumber(branchGraphData.MaxBranchThreshold, DefaultMaxBranchThreshold);
        branchGraphData.CurrentThreshold = GetPositiveNumber(branchGraphData.CurrentThreshold, branchGraphData.MaxBranchThreshold);
        branchGraphData.ComputedThreshold = GetPositiveNumber(branchGraphData.ComputedThreshold, branchGraphData.MaxBranchThreshold);
        branchGraphData.MinLongBranch = GetPositiveNumber(branchGraphData.MinLongBranch, quanta.Count / LongBranchDivisor);
        branchGraphData.ShortBranchPolicy = string.IsNullOrWhiteSpace(branchGraphData.ShortBranchPolicy)
            ? BranchAnalysisDefaults.ShortBranchPolicy
            : branchGraphData.ShortBranchPolicy;
        branchGraphData.StructurallyRejectedBranches = GetNonNegativeInteger(branchGraphData.StructurallyRejectedBranches, 0);
        branchGraphData.AntiMRemovedBranches = GetNonNegativeInteger(branchGraphData.AntiMRemovedBranches, 0);
        branchGraphData.LocalLoopRiskBranches = GetNonNegativeInteger(branchGraphData.LocalLoopRiskBranches, 0);
        branchGraphData.DeletedEdgeCount = GetNonNegativeInteger(branchGraphData.DeletedEdgeCount, 0);
        branchGraphData.BranchCount = GetNonNegativeInteger(branchGraphData.BranchCount, 0);
        branchGraphData.LastBranchPoint = GetNonNegativeInteger(branchGraphData.LastBranchPoint, 0);
        branchGraphData.LongestReach = GetPositiveNumber(branchGraphData.LongestReach, 0);
        branchGraphData.TotalBeats = quanta.Count;

        return branchGraphData;
    }

    private static bool PassesLookaheadGate(
        IReadOnlyList<TimeQuantum> quanta,
        TimeQuantum source,
        TimeQuantum destination,
        int lookaheadDepth,
        double maxThreshold)
    {
        if (lookaheadDepth <= 1)
        {
            return true;
        }

        for (int offset = 1; offset < lookaheadDepth; offset++)
        {
            int sourceIndex = source.Which + offset;
            int destinationIndex = destination.Which + offset;

            if (sourceIndex < 0
                || destinationIndex < 0
                || sourceIndex >= quanta.Count
                || destinationIndex >= quanta.Count)
            {
                return false;
            }

            double continuationDistance = CalculateQuantumDistance(
                quanta[sourceIndex],
                quanta[destinationIndex]);

            if (!double.IsFinite(continuationDistance)
                || continuationDistance > maxThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<TimeQuantum> GetQuanta(TrackAnalysisDocument track, string type)
    {
        if (track is null)
        {
            throw new NearestNeighborException("Track must be an object.");
        }

        if (track.Analysis is null)
        {
            throw new NearestNeighborException("Track analysis must be an object.");
        }

        return type switch
        {
            SectionsType => track.Analysis.Sections,
            BarsType => track.Analysis.Bars,
            BeatsType => track.Analysis.Beats,
            TatumsType => track.Analysis.Tatums,
            SegmentsType => track.Analysis.Segments,
            FilteredSegmentsType => track.Analysis.FilteredSegments,
            _ => throw new NearestNeighborException($"Track analysis.{type} must be an array.")
        };
    }

    private static int EstimateBeatsPerBar(TrackAnalysisDocument track)
    {
        foreach (TimeQuantum bar in track.Analysis.Bars)
        {
            if (bar.Children.Count > 0)
            {
                return bar.Children.Count;
            }
        }

        return DefaultEstimatedBeatsPerBar;
    }

    private static double GetBranchScore(BranchEdge edge)
    {
        if (double.IsFinite(edge.BranchScore))
        {
            return edge.BranchScore;
        }

        return double.IsFinite(edge.Distance) ? edge.Distance : double.PositiveInfinity;
    }

    private static double GetJumpFallback(BranchEdge edge)
    {
        if (edge.Source is null || edge.Destination is null)
        {
            return 0;
        }

        return Math.Abs(edge.Destination.Which - edge.Source.Which);
    }

    private static int GetDestinationWhich(BranchEdge edge)
    {
        return edge.Destination?.Which ?? int.MaxValue;
    }

    private static double GetFiniteNumber(double value, double fallback)
    {
        return double.IsFinite(value) ? value : fallback;
    }

    private static double GetFiniteNumber(double? value, double fallback)
    {
        return value.HasValue && double.IsFinite(value.Value) ? value.Value : fallback;
    }

    private static int GetPositiveInteger(int? value, int fallback)
    {
        return value is > 0 ? value.Value : fallback;
    }

    private static double GetPositiveNumber(double? value, double fallback)
    {
        return value is > 0 && double.IsFinite(value.Value) ? value.Value : fallback;
    }

    private static int GetNonNegativeInteger(int value, int fallback)
    {
        return value >= 0 ? value : fallback;
    }
}

