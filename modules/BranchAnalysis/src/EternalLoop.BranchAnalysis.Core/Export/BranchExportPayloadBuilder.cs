using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.Branching;
using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Export;

public static class BranchExportPayloadBuilder
{
    public const string BranchExportSchemaVersion = "eternalloop-branch-export-v1";
    public const string SourcePage = "csharp://eternalloop-branch-analysis-cli";
    public const string DefaultQuantumType = NearestNeighborCalculator.BeatsType;
    public const string UnknownTitle = "Unknown Title";
    public const string UnknownTitleMarker = "(unknown title)";
    public const string UnknownArtistMarker = "(unknown artist)";
    public const string UndefinedMarker = "undefined";
    public const string ThresholdGate = "acoustic-distance";
    public const string StructuralModeEnabled = "filter-only";
    public const string StructuralModeDisabled = "legacy-acoustic";
    public const string ActiveStatus = "active";
    public const string CandidateStatus = "candidate";
    public const string BackwardDirection = "backward";
    public const string ForwardDirection = "forward";
    public const string SelfDirection = "self";

    public static double? SafeNumber(double value)
    {
        return double.IsFinite(value) ? value : null;
    }

    public static int? SafeNumber(int value)
    {
        return value;
    }

    public static string? SafeString(object? value)
    {
        return value?.ToString();
    }

    public static int GetArrayCount<T>(IReadOnlyCollection<T>? value)
    {
        return value?.Count ?? 0;
    }

    public static BranchExportSegment? SanitizeSegmentForExport(SegmentQuantum? segment)
    {
        if (segment is null)
        {
            return null;
        }

        return new BranchExportSegment
        {
            Which = SafeNumber(segment.Which),
            Start = SafeNumber(segment.Start),
            Duration = SafeNumber(segment.Duration),
            Confidence = SafeNumber(segment.Confidence),
            LoudnessStart = SafeNumber(segment.LoudnessStart),
            LoudnessMax = SafeNumber(segment.LoudnessMax),
            LoudnessMaxTime = SafeNumber(segment.LoudnessMaxTime)
        };
    }

    public static List<BranchExportSegment> SanitizeOverlappingSegmentsForExport(TimeQuantum? quantum)
    {
        List<BranchExportSegment> results = [];

        if (quantum is null)
        {
            return results;
        }

        foreach (SegmentQuantum segment in quantum.OverlappingSegments)
        {
            BranchExportSegment? sanitized = SanitizeSegmentForExport(segment);

            if (sanitized is not null)
            {
                results.Add(sanitized);
            }
        }

        return results;
    }

    public static BranchExportQuantum? SanitizeQuantumForExport(TimeQuantum? quantum)
    {
        if (quantum is null)
        {
            return null;
        }

        return new BranchExportQuantum
        {
            Which = SafeNumber(quantum.Which),
            Start = SafeNumber(quantum.Start),
            Duration = SafeNumber(quantum.Duration),
            Confidence = SafeNumber(quantum.Confidence),
            IndexInParent = SafeNumber(quantum.IndexInParent),
            OverlappingSegmentCount = GetArrayCount(quantum.OverlappingSegments),
            OverlappingSegments = SanitizeOverlappingSegmentsForExport(quantum)
        };
    }

    public static BranchExportQuality? SanitizeBranchQualityForExport(BranchEdge? edge)
    {
        if (edge is null)
        {
            return null;
        }

        double acousticDistance = double.IsFinite(edge.AcousticDistance) ? edge.AcousticDistance : edge.Distance;
        double branchScore = double.IsFinite(edge.BranchScore) ? edge.BranchScore : edge.Distance;
        double structuralBonusDiagnosticOnly = double.IsFinite(edge.StructuralBonusDiagnosticOnly)
            ? edge.StructuralBonusDiagnosticOnly
            : edge.StructuralBonus;

        return new BranchExportQuality
        {
            AcousticDistance = SafeNumber(acousticDistance),
            BranchScore = SafeNumber(branchScore),
            StructuralPenalty = SafeNumber(edge.StructuralPenalty),
            StructuralBonus = SafeNumber(edge.StructuralBonus),
            StructuralBonusDiagnosticOnly = SafeNumber(structuralBonusDiagnosticOnly),
            ThresholdGate = ThresholdGate,
            JumpBeatsAbs = SafeNumber(edge.JumpBeatsAbs),
            JumpBars = SafeNumber(edge.JumpBars),
            SameBarPhase = edge.SameBarPhase,
            SamePhrase4Phase = edge.SamePhrasePhase4,
            SamePhrase8Phase = edge.SamePhrasePhase8,
            SamePhrase16Phase = edge.SamePhrasePhase16,
            SectionChange = edge.SectionChange,
            ShortLocalRisk = edge.ShortLocalRisk,
            LocalLoopRisk = edge.LocalLoopRisk,
            PolicyDecision = SafeString(string.IsNullOrWhiteSpace(edge.PolicyDecision) ? "legacy" : edge.PolicyDecision),
            PolicyReasons = edge.PolicyReasons.Select(reason => SafeString(reason) ?? string.Empty).ToList()
        };
    }

    public static BranchExportBranch SanitizeBranchForExport(BranchEdge? edge, TimeQuantum? sourceQuantum, string status)
    {
        TimeQuantum? source = edge?.Source ?? sourceQuantum;
        TimeQuantum? destination = edge?.Destination;
        int? fromBeat = source is null ? null : SafeNumber(source.Which);
        int? toBeat = destination is null ? null : SafeNumber(destination.Which);
        int? jumpBeats = null;
        string? direction = null;

        if (fromBeat.HasValue && toBeat.HasValue)
        {
            jumpBeats = toBeat.Value - fromBeat.Value;
            direction = jumpBeats.Value < 0
                ? BackwardDirection
                : jumpBeats.Value > 0
                    ? ForwardDirection
                    : SelfDirection;
        }

        return new BranchExportBranch
        {
            Id = edge is null ? null : SafeNumber(edge.Id),
            Status = status,
            FromBeat = fromBeat,
            ToBeat = toBeat,
            JumpBeats = jumpBeats,
            Direction = direction,
            Distance = edge is null ? null : SafeNumber(edge.Distance),
            Quality = SanitizeBranchQualityForExport(edge),
            Deleted = edge?.Deleted == true,
            Source = SanitizeQuantumForExport(source),
            Destination = SanitizeQuantumForExport(destination)
        };
    }

    public static int CompareBranchesForExport(BranchExportBranch? left, BranchExportBranch? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return 1;
        }

        if (right is null)
        {
            return -1;
        }

        int fromCompare = CompareNullableNumbers(left.FromBeat, right.FromBeat);

        if (fromCompare != 0)
        {
            return fromCompare;
        }

        int toCompare = CompareNullableNumbers(left.ToBeat, right.ToBeat);

        if (toCompare != 0)
        {
            return toCompare;
        }

        return CompareNullableNumbers(left.Distance, right.Distance);
    }

    public static List<BranchExportBranch> CollectActiveBranchesForExport(
        TrackAnalysisDocument track,
        string type = DefaultQuantumType)
    {
        List<BranchExportBranch> results = [];

        foreach (TimeQuantum quantum in GetQuantaOrEmpty(track, type))
        {
            foreach (BranchEdge neighbor in quantum.Neighbors)
            {
                results.Add(SanitizeBranchForExport(neighbor, quantum, ActiveStatus));
            }
        }

        results.Sort(CompareBranchesForExport);
        return results;
    }

    public static List<BranchExportBranch> CollectCandidateBranchesForExport(
        TrackAnalysisDocument track,
        string type = DefaultQuantumType)
    {
        List<BranchExportBranch> results = [];

        foreach (TimeQuantum quantum in GetQuantaOrEmpty(track, type))
        {
            foreach (BranchEdge neighbor in quantum.AllNeighbors)
            {
                results.Add(SanitizeBranchForExport(neighbor, quantum, CandidateStatus));
            }
        }

        results.Sort(CompareBranchesForExport);
        return results;
    }

    public static BranchExportPayload Build(
        TrackAnalysisDocument track,
        BranchGraphData branchGraphData,
        string type = DefaultQuantumType)
    {
        IReadOnlyList<TimeQuantum> _ = GetQuanta(track, type);

        if (branchGraphData is null)
        {
            throw new BranchExportException("BranchGraphData must be an object.");
        }

        List<BranchExportBranch> activeBranches = CollectActiveBranchesForExport(track, type);
        List<BranchExportBranch> candidateBranches = CollectCandidateBranchesForExport(track, type);
        string? title = SafeString(track.Info.Title ?? track.Info.Name);
        string? artist = SafeString(track.Info.Artist);
        int localLoopRiskBranches = branchGraphData.LocalLoopRiskBranches >= 0
            ? branchGraphData.LocalLoopRiskBranches
            : CountRiskBranches(activeBranches);

        return new BranchExportPayload
        {
            SchemaVersion = BranchExportSchemaVersion,
            ExportedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            SourcePage = SourcePage,
            BranchSource = type == DefaultQuantumType
                ? "track.analysis.beats[*].neighbors"
                : $"track.analysis.{type}[*].neighbors",
            Track = new BranchExportTrack
            {
                Id = SafeString(track.Info.Id),
                Title = title,
                Artist = artist,
                FixedTitle = SafeString(track.FixedTitle ?? GetTitle(title, artist)),
                Duration = SafeNumber(track.AudioSummary.Duration)
            },
            Tuning = new BranchExportTuning
            {
                QuantumType = type,
                CurrentThreshold = SafeNumber(branchGraphData.CurrentThreshold),
                ComputedThreshold = SafeNumber(branchGraphData.ComputedThreshold),
                MaxBranches = SafeNumber(branchGraphData.MaxBranches),
                SimilarityThreshold = SafeNumber(branchGraphData.SimilarityThreshold),
                LookaheadDepth = SafeNumber(branchGraphData.LookaheadDepth),
                MinJumpDistance = SafeNumber(branchGraphData.MinJumpDistance),
                MaxBranchThreshold = SafeNumber(branchGraphData.MaxBranchThreshold),
                AddLastEdge = branchGraphData.AddLastEdge,
                JustBackwards = branchGraphData.JustBackwards,
                JustLongBranches = branchGraphData.JustLongBranches,
                RemoveSequentialBranches = branchGraphData.RemoveSequentialBranches,
                MinLongBranch = SafeNumber(branchGraphData.MinLongBranch),
                LastBranchPoint = SafeNumber(branchGraphData.LastBranchPoint),
                LongestReach = SafeNumber(branchGraphData.LongestReach),
                StructuralPolicy = branchGraphData.StructuralPolicy,
                AntiLocalLoopPolicy = branchGraphData.AntiLocalLoopPolicy,
                ShortBranchPolicy = SafeString(branchGraphData.ShortBranchPolicy),
                ScoreGate = ThresholdGate,
                StructuralMode = branchGraphData.StructuralPolicy ? StructuralModeEnabled : StructuralModeDisabled,
                LateAnchorRouting = branchGraphData.LateAnchorRouting,
                EarlyReturnTargetPercent = SafeNumber(branchGraphData.EarlyReturnTargetPercent),
                LateAnchorPreferredStartPercent = SafeNumber(branchGraphData.LateAnchorPreferredStartPercent),
                LateAnchorFallbackStartPercent = SafeNumber(branchGraphData.LateAnchorFallbackStartPercent)
            },
            Policy = StructuralBranchPolicy.GetPolicySummary(branchGraphData.StructuralContext),
            Counts = new BranchExportCounts
            {
                Sections = GetArrayCount(track.Analysis.Sections),
                Bars = GetArrayCount(track.Analysis.Bars),
                Beats = GetArrayCount(track.Analysis.Beats),
                Tatums = GetArrayCount(track.Analysis.Tatums),
                Segments = GetArrayCount(track.Analysis.Segments),
                ActiveBranches = activeBranches.Count,
                CandidateBranches = candidateBranches.Count,
                VisualBranchCount = SafeNumber(branchGraphData.BranchCount),
                DeletedBranches = SafeNumber(branchGraphData.DeletedEdgeCount),
                ShortActiveBranches = CountBranchesByJump(activeBranches, branchGraphData.StructuralContext, "short"),
                VeryShortActiveBranches = CountBranchesByJump(activeBranches, branchGraphData.StructuralContext, "veryShort"),
                LocalLoopRiskBranches = localLoopRiskBranches,
                StructurallyRejectedBranches = Math.Max(0, branchGraphData.StructurallyRejectedBranches),
                AntiMRemovedBranches = Math.Max(0, branchGraphData.AntiMRemovedBranches)
            },
            Diagnostics = new BranchExportDiagnostics
            {
                StructurallyRejectedBranches = Math.Max(0, branchGraphData.StructurallyRejectedBranches),
                AntiMRemovedBranches = Math.Max(0, branchGraphData.AntiMRemovedBranches),
                LocalLoopRiskBranches = localLoopRiskBranches,
                LateAnchorDecision = SafeString(branchGraphData.LateAnchorDecision) ?? string.Empty,
                LateAnchorReason = SafeString(branchGraphData.LateAnchorReason) ?? string.Empty,
                LateAnchorEarlyReturnTargetBeat = SafeNonNegativeNumber(branchGraphData.LateAnchorEarlyReturnTargetBeat),
                LateAnchorBranchesToTarget = SafeNonNegativeNumber(branchGraphData.LateAnchorBranchesToTarget),
                LateAnchorEarliestReachableBeat = SafeNonNegativeNumber(branchGraphData.LateAnchorEarliestReachableBeat),
                LateAnchorImmediateBackwardBeats = SafeNonNegativeNumber(branchGraphData.LateAnchorImmediateBackwardBeats),
                LateAnchorDistance = SafeNumber(branchGraphData.LateAnchorDistance),
                LateAnchorInsertedEdgeId = SafeNonNegativeNumber(branchGraphData.LateAnchorInsertedEdgeId),
                LateAnchorSelectedEdgeId = SafeNonNegativeNumber(branchGraphData.LateAnchorSelectedEdgeId)
            },
            ActiveBranches = activeBranches,
            CandidateBranches = candidateBranches
        };
    }

    public static string GetTitle(string? title, string? artist)
    {
        string? fixedTitle = title;

        if (string.IsNullOrEmpty(fixedTitle)
            || fixedTitle == UnknownTitleMarker
            || fixedTitle == UndefinedMarker)
        {
            fixedTitle = UnknownTitle;
        }
        else if (!string.IsNullOrEmpty(artist) && artist != UnknownArtistMarker)
        {
            fixedTitle = $"{fixedTitle} by {artist}";
        }

        return fixedTitle;
    }

    private static int? SafeNonNegativeNumber(int value)
    {
        return value >= 0 ? value : null;
    }

    public static JsonNode ToJsonNode(BranchExportPayload payload)
    {
        return JsonSerializer.SerializeToNode(payload) ?? throw new BranchExportException("Branch export payload could not be serialized.");
    }

    private static int CountBranchesByJump(
        IReadOnlyList<BranchExportBranch> branches,
        StructuralBranchContext? context,
        string kind)
    {
        if (context is null)
        {
            return 0;
        }

        int limit = kind == "veryShort" ? context.VeryShortJumpBeats : context.ShortJumpBeats;
        int count = 0;

        foreach (BranchExportBranch branch in branches)
        {
            if (branch.JumpBeats.HasValue && Math.Abs(branch.JumpBeats.Value) < limit)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountRiskBranches(IReadOnlyList<BranchExportBranch> branches)
    {
        return branches.Count(branch => branch.Quality?.LocalLoopRisk == true);
    }

    private static int CompareNullableNumbers(int? left, int? right)
    {
        if (left == right)
        {
            return 0;
        }

        if (!left.HasValue)
        {
            return 1;
        }

        if (!right.HasValue)
        {
            return -1;
        }

        return left.Value.CompareTo(right.Value);
    }

    private static int CompareNullableNumbers(double? left, double? right)
    {
        if (left == right)
        {
            return 0;
        }

        if (!left.HasValue)
        {
            return 1;
        }

        if (!right.HasValue)
        {
            return -1;
        }

        return left.Value.CompareTo(right.Value);
    }

    private static IReadOnlyList<TimeQuantum> GetQuantaOrEmpty(TrackAnalysisDocument? track, string type)
    {
        if (track?.Analysis is null)
        {
            return [];
        }

        return type switch
        {
            NearestNeighborCalculator.SectionsType => track.Analysis.Sections,
            NearestNeighborCalculator.BarsType => track.Analysis.Bars,
            NearestNeighborCalculator.BeatsType => track.Analysis.Beats,
            NearestNeighborCalculator.TatumsType => track.Analysis.Tatums,
            NearestNeighborCalculator.SegmentsType => track.Analysis.Segments,
            NearestNeighborCalculator.FilteredSegmentsType => track.Analysis.FilteredSegments,
            _ => []
        };
    }

    private static IReadOnlyList<TimeQuantum> GetQuanta(TrackAnalysisDocument track, string type)
    {
        if (track is null)
        {
            throw new BranchExportException("Track must be an object.");
        }

        if (track.Analysis is null)
        {
            throw new BranchExportException("Track analysis must be an object.");
        }

        return type switch
        {
            NearestNeighborCalculator.SectionsType => track.Analysis.Sections,
            NearestNeighborCalculator.BarsType => track.Analysis.Bars,
            NearestNeighborCalculator.BeatsType => track.Analysis.Beats,
            NearestNeighborCalculator.TatumsType => track.Analysis.Tatums,
            NearestNeighborCalculator.SegmentsType => track.Analysis.Segments,
            NearestNeighborCalculator.FilteredSegmentsType => track.Analysis.FilteredSegments,
            _ => throw new BranchExportException($"Track analysis.{type} must be an array.")
        };
    }
}

