using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public static class BranchTopologyPolicy
{
    public const string DefaultQuantumType = NearestNeighborCalculator.BeatsType;

    public static BranchTopologyResult ReduceLocalLoopBranches(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type = DefaultQuantumType,
        StructuralBranchContext? context = null,
        StructuralBranchOptions? options = null)
    {
        StructuralBranchOptions policyOptions = options ?? context?.Options ?? StructuralBranchOptions.FromBranchGraphData(branchGraphData);

        if (!policyOptions.AntiLocalLoopPolicy)
        {
            branchGraphData.AntiMRemovedBranches = ToNonNegativeInteger(branchGraphData.AntiMRemovedBranches, 0);
            return new BranchTopologyResult
            {
                RemovedBranches = 0,
                LocalLoopRiskBranches = CountLocalLoopRiskBranches(track, type)
            };
        }

        StructuralBranchContext policyContext = context
            ?? StructuralBranchPolicy.BuildStructuralBranchContext(track, type, policyOptions);
        Dictionary<string, List<BranchEdge>> clusters = CollectShortLocalClusters(track, type, policyContext);
        int removedBranches = 0;

        foreach (List<BranchEdge> cluster in clusters.Values)
        {
            List<BranchEdge> removals = ChooseClusterRemovals(cluster, policyOptions.MaxShortLocalBranchesPerCluster);

            foreach (BranchEdge edge in removals)
            {
                if (RemoveActiveEdge(edge, branchGraphData))
                {
                    removedBranches++;
                }
            }
        }

        branchGraphData.AntiMRemovedBranches = ToNonNegativeInteger(branchGraphData.AntiMRemovedBranches, 0) + removedBranches;
        branchGraphData.LocalLoopRiskBranches = CountLocalLoopRiskBranches(track, type);

        return new BranchTopologyResult
        {
            RemovedBranches = removedBranches,
            LocalLoopRiskBranches = branchGraphData.LocalLoopRiskBranches
        };
    }

    public static Dictionary<string, List<BranchEdge>> CollectShortLocalClusters(
        TrackAnalysisDocument track,
        string type,
        StructuralBranchContext context)
    {
        Dictionary<string, List<BranchEdge>> clusters = [];
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        int windowBeats = Math.Max(1, context.PhraseWindowBeats);

        foreach (TimeQuantum source in quanta)
        {
            foreach (BranchEdge edge in source.Neighbors)
            {
                if (!IsClusterableLocalEdge(edge, context))
                {
                    continue;
                }

                int sourceWindow = (int)Math.Floor(edge.Source!.Which / (double)windowBeats);
                int destinationWindow = (int)Math.Floor(edge.Destination!.Which / (double)windowBeats);
                string key = $"{sourceWindow}:{destinationWindow}";

                if (!clusters.TryGetValue(key, out List<BranchEdge>? cluster))
                {
                    cluster = [];
                    clusters[key] = cluster;
                }

                cluster.Add(edge);
            }
        }

        foreach (string key in clusters.Where(pair => pair.Value.Count < 2).Select(pair => pair.Key).ToArray())
        {
            clusters.Remove(key);
        }

        return clusters;
    }

    public static List<BranchEdge> ChooseClusterRemovals(IReadOnlyList<BranchEdge> cluster, int maxKept)
    {
        if (cluster.Count == 0)
        {
            return [];
        }

        if (!cluster.Any(HasStructuralEvidence))
        {
            return cluster.ToList();
        }

        List<BranchEdge> sorted = cluster.ToList();
        sorted.Sort(CompareEdgesByQuality);
        HashSet<BranchEdge> kept = sorted.Take(Math.Max(0, maxKept)).ToHashSet();

        return cluster.Where(edge => !kept.Contains(edge)).ToList();
    }

    public static int CountLocalLoopRiskBranches(
        TrackAnalysisDocument track,
        string type = DefaultQuantumType)
    {
        int count = 0;

        foreach (TimeQuantum quantum in GetQuanta(track, type))
        {
            foreach (BranchEdge edge in quantum.Neighbors)
            {
                if (!edge.Deleted && edge.LocalLoopRisk)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool IsClusterableLocalEdge(BranchEdge? edge, StructuralBranchContext context)
    {
        if (edge is null || edge.Deleted || edge.Source is null || edge.Destination is null)
        {
            return false;
        }

        if (edge.JumpBeatsAbs >= context.ShortJumpBeats)
        {
            return false;
        }

        if (edge.SectionChange)
        {
            return false;
        }

        if (edge.ShortLocalRisk || edge.LocalLoopRisk)
        {
            return true;
        }

        int windowBeats = Math.Max(1, context.PhraseWindowBeats);
        int sourceWindow = (int)Math.Floor(edge.Source.Which / (double)windowBeats);
        int destinationWindow = (int)Math.Floor(edge.Destination.Which / (double)windowBeats);

        return sourceWindow == destinationWindow;
    }

    private static bool HasStructuralEvidence(BranchEdge edge)
    {
        return edge.SectionChange
            || edge.PolicyReasons.Contains("section-change")
            || edge.PolicyReasons.Contains("structural-boundary");
    }

    private static bool RemoveActiveEdge(BranchEdge edge, BranchGraphData branchGraphData)
    {
        if (edge.Deleted)
        {
            return false;
        }

        edge.Deleted = true;
        edge.LocalLoopRisk = true;

        if (edge.PolicyDecision == "accepted")
        {
            edge.PolicyDecision = "removed-local-loop";
        }

        if (!edge.PolicyReasons.Contains("anti-m-removed"))
        {
            edge.PolicyReasons.Add("anti-m-removed");
        }

        edge.Source?.Neighbors.Remove(edge);
        branchGraphData.DeletedEdgeCount = ToNonNegativeInteger(branchGraphData.DeletedEdgeCount, 0) + 1;

        return true;
    }

    private static int CompareEdgesByQuality(BranchEdge left, BranchEdge right)
    {
        double leftScore = GetSortableNumber(left.BranchScore, left.Distance);
        double rightScore = GetSortableNumber(right.BranchScore, right.Distance);

        if (leftScore != rightScore)
        {
            return leftScore.CompareTo(rightScore);
        }

        double leftJump = GetSortableNumber(left.JumpBeatsAbs, GetJumpFallback(left));
        double rightJump = GetSortableNumber(right.JumpBeatsAbs, GetJumpFallback(right));

        if (leftJump != rightJump)
        {
            return rightJump.CompareTo(leftJump);
        }

        return (left.Destination?.Which ?? int.MaxValue).CompareTo(right.Destination?.Which ?? int.MaxValue);
    }

    private static IReadOnlyList<TimeQuantum> GetQuanta(TrackAnalysisDocument track, string type)
    {
        if (track is null)
        {
            throw new BranchTopologyPolicyException("Track must be an object.");
        }

        if (track.Analysis is null)
        {
            throw new BranchTopologyPolicyException("Track analysis must be an object.");
        }

        return type switch
        {
            NearestNeighborCalculator.SectionsType => track.Analysis.Sections,
            NearestNeighborCalculator.BarsType => track.Analysis.Bars,
            NearestNeighborCalculator.BeatsType => track.Analysis.Beats,
            NearestNeighborCalculator.TatumsType => track.Analysis.Tatums,
            NearestNeighborCalculator.SegmentsType => track.Analysis.Segments,
            NearestNeighborCalculator.FilteredSegmentsType => track.Analysis.FilteredSegments,
            _ => throw new BranchTopologyPolicyException($"Track analysis.{type} must be an array.")
        };
    }

    private static double GetSortableNumber(double value, double fallback)
    {
        if (double.IsFinite(value))
        {
            return value;
        }

        return double.IsFinite(fallback) ? fallback : double.PositiveInfinity;
    }

    private static double GetJumpFallback(BranchEdge edge)
    {
        if (edge.Source is null || edge.Destination is null)
        {
            return 0;
        }

        return Math.Abs(edge.Destination.Which - edge.Source.Which);
    }

    private static int ToNonNegativeInteger(int value, int fallback)
    {
        return value >= 0 ? value : fallback;
    }
}

