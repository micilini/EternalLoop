using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Branching;

public static class StructuralBranchPolicy
{
    public const string PolicyName = "structural-branch-utility-v1";
    public const double DefaultExceptionalAcousticDistance = 8;
    public const double VeryShortJumpPenalty = 35;
    public const double ShortJumpPenalty = 18;
    public const double LongJumpDiagnosticBonus = 12;
    public const double SameBarPhaseShortDiagnosticBonus = 4;
    public const double SameBarPhaseDiagnosticBonus = 8;
    public const double ShortBarPhaseMismatchPenalty = 18;
    public const double BarPhaseMismatchPenalty = 8;
    public const double Phrase4PhaseMismatchPenalty = 8;
    public const double PhrasePhaseDiagnosticBonus = 3;
    public const double SectionChangeDiagnosticBonus = 12;
    public const double StructuralBoundaryDiagnosticBonus = 8;
    public const double ReachGainDiagnosticBonus = 8;
    public const double ShortLocalRiskPenalty = 28;
    public const double LocalLoopRiskPenalty = 18;
    public const double ExceptionalAcousticDiagnosticBonus = 4;
    public const int DefaultBeatsPerBar = 4;

    public static StructuralBranchContext BuildStructuralBranchContext(
        TrackAnalysisDocument track,
        string type = NearestNeighborCalculator.BeatsType,
        StructuralBranchOptions? options = null)
    {
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, type);
        StructuralBranchOptions policyOptions = options ?? new StructuralBranchOptions();
        int beatsPerBar = EstimateBeatsPerBar(track);
        IReadOnlyList<TimeQuantum> bars = track.Analysis.Bars;
        Dictionary<TimeQuantum, StructuralBeatContext> beatContexts = [];

        foreach (TimeQuantum quantum in quanta)
        {
            TimeQuantum? bar = quantum.Parent;
            TimeQuantum? section = bar?.Parent;
            int barIndex = GetQuantumIndex(bar, bars);
            int sectionBars = section?.Children.Count ?? 0;
            int barInSection = GetIndexInParent(bar);
            int beatIndex = ToNonNegativeInteger(quantum.Which, 0);
            int beatInBar = quantum.IndexInParent >= 0 ? quantum.IndexInParent : beatIndex % beatsPerBar;

            beatContexts[quantum] = new StructuralBeatContext
            {
                Quantum = quantum,
                BeatIndex = beatIndex,
                BarIndex = barIndex,
                SectionIndex = GetIndexInParent(section),
                BeatInBar = beatInBar,
                BarInSection = barInSection,
                BeatsPerBar = beatsPerBar,
                Phrase4Index = (int)Math.Floor(barIndex / 4.0),
                Phrase8Index = (int)Math.Floor(barIndex / 8.0),
                Phrase16Index = (int)Math.Floor(barIndex / 16.0),
                Phrase4Phase = PositiveModulo(barIndex, 4),
                Phrase8Phase = PositiveModulo(barIndex, 8),
                Phrase16Phase = PositiveModulo(barIndex, 16),
                NearSectionBoundary = IsNearSectionBoundary(barInSection, sectionBars, policyOptions.LocalWindowBars),
                NearBarBoundary = beatInBar == 0 || beatInBar == beatsPerBar - 1,
                Confidence = ToFiniteNumber(quantum.Confidence, 0)
            };
        }

        return new StructuralBranchContext
        {
            Name = PolicyName,
            Enabled = policyOptions.StructuralPolicy,
            Options = policyOptions,
            BeatsPerBar = beatsPerBar,
            TotalBeats = quanta.Count,
            VeryShortJumpBeats = beatsPerBar * policyOptions.VeryShortBars,
            ShortJumpBeats = beatsPerBar * policyOptions.ShortBars,
            PhraseWindowBeats = beatsPerBar * policyOptions.PhraseBars,
            BeatContexts = beatContexts,
            StructurallyRejectedBranches = 0
        };
    }

    public static StructuralBranchContext BuildStructuralBranchContext(
        TrackAnalysisDocument track,
        string type,
        BranchGraphData data)
    {
        return BuildStructuralBranchContext(track, type, StructuralBranchOptions.FromBranchGraphData(data));
    }

    public static StructuralBranchScore ScoreBranchCandidate(
        TimeQuantum sourceQuantum,
        TimeQuantum destinationQuantum,
        double acousticDistance,
        StructuralBranchContext context,
        StructuralBranchOptions? options = null)
    {
        StructuralBranchOptions policyOptions = options ?? context.Options;
        StructuralBeatContext source = GetQuantumContext(context, sourceQuantum);
        StructuralBeatContext destination = GetQuantumContext(context, destinationQuantum);
        int jumpBeats = destinationQuantum.Which - sourceQuantum.Which;
        double jumpBeatsAbs = Math.Abs(jumpBeats);
        double jumpBars = jumpBeatsAbs / Math.Max(1, source.BeatsPerBar);
        bool sameBarPhase = source.BeatInBar == destination.BeatInBar;
        bool samePhrasePhase4 = source.Phrase4Phase == destination.Phrase4Phase;
        bool samePhrasePhase8 = source.Phrase8Phase == destination.Phrase8Phase;
        bool samePhrasePhase16 = source.Phrase16Phase == destination.Phrase16Phase;
        bool sectionChange = source.SectionIndex != destination.SectionIndex;
        bool nearStructuralBoundary = source.NearSectionBoundary || destination.NearSectionBoundary;
        bool reachGain = jumpBeatsAbs >= Math.Max(context.ShortJumpBeats, Math.Floor(context.TotalBeats / 2.0));
        bool shortLocalRisk = jumpBeatsAbs < context.ShortJumpBeats
            && !sectionChange
            && !nearStructuralBoundary
            && !reachGain;
        bool localLoopRisk = shortLocalRisk
            && (source.Phrase4Index == destination.Phrase4Index || source.Phrase8Index == destination.Phrase8Index);
        List<string> reasons = [];
        double structuralPenalty = 0;
        double structuralBonusDiagnosticOnly = 0;
        string policyDecision = "accepted";

        if (!policyOptions.StructuralPolicy)
        {
            return new StructuralBranchScore
            {
                AcousticDistance = acousticDistance,
                StructuralPenalty = 0,
                StructuralBonus = 0,
                StructuralBonusDiagnosticOnly = 0,
                BranchScore = acousticDistance,
                JumpBeatsAbs = jumpBeatsAbs,
                JumpBars = jumpBars,
                SameBarPhase = sameBarPhase,
                SamePhrasePhase4 = samePhrasePhase4,
                SamePhrasePhase8 = samePhrasePhase8,
                SamePhrasePhase16 = samePhrasePhase16,
                SectionChange = sectionChange,
                NearStructuralBoundary = nearStructuralBoundary,
                ShortLocalRisk = false,
                LocalLoopRisk = false,
                PolicyDecision = "legacy",
                PolicyReasons = ["structural-policy-disabled"]
            };
        }

        if (jumpBeatsAbs < context.VeryShortJumpBeats)
        {
            structuralPenalty += VeryShortJumpPenalty;
            reasons.Add("very-short-jump");
        }
        else if (jumpBeatsAbs < context.ShortJumpBeats)
        {
            structuralPenalty += ShortJumpPenalty;
            reasons.Add("short-jump");
        }
        else if (jumpBeatsAbs >= context.PhraseWindowBeats)
        {
            structuralBonusDiagnosticOnly += LongJumpDiagnosticBonus;
            reasons.Add("long-jump");
        }
        else
        {
            reasons.Add("medium-jump");
        }

        if (sameBarPhase)
        {
            structuralBonusDiagnosticOnly += jumpBeatsAbs < context.ShortJumpBeats
                ? SameBarPhaseShortDiagnosticBonus
                : SameBarPhaseDiagnosticBonus;
            reasons.Add("same-bar-phase");
        }
        else
        {
            structuralPenalty += jumpBeatsAbs < context.ShortJumpBeats
                ? ShortBarPhaseMismatchPenalty
                : BarPhaseMismatchPenalty;
            reasons.Add("bar-phase-mismatch");
        }

        if (samePhrasePhase4)
        {
            structuralBonusDiagnosticOnly += PhrasePhaseDiagnosticBonus;
            reasons.Add("same-phrase4-phase");
        }
        else if (jumpBeatsAbs < context.ShortJumpBeats)
        {
            structuralPenalty += Phrase4PhaseMismatchPenalty;
            reasons.Add("phrase4-phase-mismatch");
        }

        if (samePhrasePhase8)
        {
            structuralBonusDiagnosticOnly += PhrasePhaseDiagnosticBonus;
            reasons.Add("same-phrase8-phase");
        }

        if (samePhrasePhase16 && jumpBeatsAbs >= context.PhraseWindowBeats)
        {
            structuralBonusDiagnosticOnly += PhrasePhaseDiagnosticBonus;
            reasons.Add("same-phrase16-phase");
        }

        if (sectionChange)
        {
            structuralBonusDiagnosticOnly += SectionChangeDiagnosticBonus;
            reasons.Add("section-change");
        }

        if (nearStructuralBoundary)
        {
            structuralBonusDiagnosticOnly += StructuralBoundaryDiagnosticBonus;
            reasons.Add("structural-boundary");
        }

        if (reachGain)
        {
            structuralBonusDiagnosticOnly += ReachGainDiagnosticBonus;
            reasons.Add("reach-gain");
        }

        if (shortLocalRisk)
        {
            structuralPenalty += ShortLocalRiskPenalty;
            reasons.Add("short-local-risk");
        }

        if (localLoopRisk)
        {
            structuralPenalty += LocalLoopRiskPenalty;
            reasons.Add("local-loop-risk");
        }

        if (double.IsFinite(acousticDistance) && acousticDistance <= policyOptions.ExceptionalAcousticDistance)
        {
            structuralBonusDiagnosticOnly += ExceptionalAcousticDiagnosticBonus;
            reasons.Add("exceptional-acoustic-match");
        }

        double evidenceCredit = Math.Min(structuralPenalty, structuralBonusDiagnosticOnly);
        structuralPenalty = Math.Max(0, structuralPenalty - evidenceCredit);
        bool veryShortEvidence = (sectionChange || nearStructuralBoundary || reachGain)
            && sameBarPhase
            && acousticDistance <= policyOptions.ExceptionalAcousticDistance
            && !localLoopRisk;
        bool shortEvidence = (sectionChange || nearStructuralBoundary || reachGain)
            && sameBarPhase
            && !localLoopRisk;

        if (jumpBeatsAbs < context.VeryShortJumpBeats && !veryShortEvidence)
        {
            policyDecision = "rejected";
            reasons.Add("rejected-very-short-local");
        }
        else if (jumpBeatsAbs < context.ShortJumpBeats && shortLocalRisk && !shortEvidence)
        {
            policyDecision = "rejected";
            reasons.Add("rejected-short-local");
        }

        return new StructuralBranchScore
        {
            AcousticDistance = acousticDistance,
            StructuralPenalty = structuralPenalty,
            StructuralBonus = structuralBonusDiagnosticOnly,
            StructuralBonusDiagnosticOnly = structuralBonusDiagnosticOnly,
            BranchScore = acousticDistance + structuralPenalty,
            JumpBeatsAbs = jumpBeatsAbs,
            JumpBars = jumpBars,
            SameBarPhase = sameBarPhase,
            SamePhrasePhase4 = samePhrasePhase4,
            SamePhrasePhase8 = samePhrasePhase8,
            SamePhrasePhase16 = samePhrasePhase16,
            SectionChange = sectionChange,
            NearStructuralBoundary = nearStructuralBoundary,
            ShortLocalRisk = shortLocalRisk,
            LocalLoopRisk = localLoopRisk,
            PolicyDecision = policyDecision,
            PolicyReasons = reasons
        };
    }

    public static bool IsStructurallyAllowedBranch(
        TimeQuantum sourceQuantum,
        TimeQuantum destinationQuantum,
        StructuralBranchScore score,
        StructuralBranchContext context,
        StructuralBranchOptions? options = null)
    {
        StructuralBranchOptions policyOptions = options ?? context.Options;
        return !policyOptions.StructuralPolicy || score.PolicyDecision == "accepted";
    }

    public static bool IsShortLocalBranch(StructuralBranchScore? score)
    {
        return score is not null && score.ShortLocalRisk;
    }

    public static BranchEdge AttachScoreToEdge(BranchEdge edge, StructuralBranchScore score)
    {
        edge.AcousticDistance = score.AcousticDistance;
        edge.StructuralPenalty = score.StructuralPenalty;
        edge.StructuralBonus = score.StructuralBonus;
        edge.StructuralBonusDiagnosticOnly = score.StructuralBonusDiagnosticOnly;
        edge.BranchScore = score.BranchScore;
        edge.JumpBeatsAbs = score.JumpBeatsAbs;
        edge.JumpBars = score.JumpBars;
        edge.SameBarPhase = score.SameBarPhase;
        edge.SamePhrasePhase4 = score.SamePhrasePhase4;
        edge.SamePhrasePhase8 = score.SamePhrasePhase8;
        edge.SamePhrasePhase16 = score.SamePhrasePhase16;
        edge.SectionChange = score.SectionChange;
        edge.NearStructuralBoundary = score.NearStructuralBoundary;
        edge.ShortLocalRisk = score.ShortLocalRisk;
        edge.LocalLoopRisk = score.LocalLoopRisk;
        edge.PolicyDecision = score.PolicyDecision;
        edge.PolicyReasons = score.PolicyReasons;

        return edge;
    }

    public static StructuralPolicySummary GetPolicySummary(StructuralBranchContext? context)
    {
        return new StructuralPolicySummary
        {
            Name = PolicyName,
            Enabled = context?.Enabled ?? BranchAnalysisDefaults.StructuralPolicy,
            ShortBranchPolicy = BranchAnalysisDefaults.ShortBranchPolicy,
            AntiLocalLoopPolicy = context?.Options.AntiLocalLoopPolicy ?? BranchAnalysisDefaults.AntiLocalLoopPolicy,
            MinVeryShortJumpBeats = context?.VeryShortJumpBeats,
            MinShortJumpBeats = context?.ShortJumpBeats,
            PhraseWindowBeats = context?.PhraseWindowBeats
        };
    }

    private static StructuralBeatContext GetQuantumContext(StructuralBranchContext context, TimeQuantum quantum)
    {
        if (context.BeatContexts.TryGetValue(quantum, out StructuralBeatContext? beatContext))
        {
            return beatContext;
        }

        int beatIndex = ToNonNegativeInteger(quantum.Which, 0);
        int beatsPerBar = context.BeatsPerBar > 0 ? context.BeatsPerBar : DefaultBeatsPerBar;
        int barIndex = (int)Math.Floor(beatIndex / (double)beatsPerBar);

        return new StructuralBeatContext
        {
            Quantum = quantum,
            BeatIndex = beatIndex,
            BarIndex = barIndex,
            SectionIndex = 0,
            BeatInBar = beatIndex % beatsPerBar,
            BarInSection = barIndex,
            BeatsPerBar = beatsPerBar,
            Phrase4Index = (int)Math.Floor(barIndex / 4.0),
            Phrase8Index = (int)Math.Floor(barIndex / 8.0),
            Phrase16Index = (int)Math.Floor(barIndex / 16.0),
            Phrase4Phase = PositiveModulo(barIndex, 4),
            Phrase8Phase = PositiveModulo(barIndex, 8),
            Phrase16Phase = PositiveModulo(barIndex, 16),
            NearSectionBoundary = false,
            NearBarBoundary = beatIndex % beatsPerBar == 0,
            Confidence = ToFiniteNumber(quantum.Confidence, 0)
        };
    }

    private static int EstimateBeatsPerBar(TrackAnalysisDocument track)
    {
        Dictionary<int, int> counts = [];

        foreach (TimeQuantum bar in track.Analysis.Bars)
        {
            if (bar.Children.Count == 0)
            {
                continue;
            }

            counts[bar.Children.Count] = counts.TryGetValue(bar.Children.Count, out int frequency)
                ? frequency + 1
                : 1;
        }

        int bestCount = DefaultBeatsPerBar;
        int bestFrequency = 0;

        foreach (KeyValuePair<int, int> pair in counts)
        {
            if (pair.Value > bestFrequency || pair.Value == bestFrequency && pair.Key > bestCount)
            {
                bestCount = pair.Key;
                bestFrequency = pair.Value;
            }
        }

        return bestCount;
    }

    private static bool IsNearSectionBoundary(int barInSection, int sectionBars, int localWindowBars)
    {
        if (barInSection < 0)
        {
            return false;
        }

        if (sectionBars <= 0)
        {
            return barInSection < localWindowBars;
        }

        return barInSection < localWindowBars || sectionBars - barInSection <= localWindowBars;
    }

    private static int GetQuantumIndex(TimeQuantum? quantum, IReadOnlyList<TimeQuantum> collection)
    {
        if (quantum is null)
        {
            return 0;
        }

        if (quantum.Which >= 0)
        {
            return quantum.Which;
        }

        for (int index = 0; index < collection.Count; index++)
        {
            if (ReferenceEquals(collection[index], quantum))
            {
                return index;
            }
        }

        return 0;
    }

    private static int GetIndexInParent(TimeQuantum? quantum)
    {
        if (quantum is null)
        {
            return 0;
        }

        return quantum.IndexInParent >= 0 ? quantum.IndexInParent : GetQuantumIndex(quantum, []);
    }

    private static IReadOnlyList<TimeQuantum> GetQuanta(TrackAnalysisDocument track, string type)
    {
        if (track is null || track.Analysis is null)
        {
            throw new StructuralBranchPolicyException($"Track analysis.{type} must be an array.");
        }

        return type switch
        {
            NearestNeighborCalculator.SectionsType => track.Analysis.Sections,
            NearestNeighborCalculator.BarsType => track.Analysis.Bars,
            NearestNeighborCalculator.BeatsType => track.Analysis.Beats,
            NearestNeighborCalculator.TatumsType => track.Analysis.Tatums,
            NearestNeighborCalculator.SegmentsType => track.Analysis.Segments,
            NearestNeighborCalculator.FilteredSegmentsType => track.Analysis.FilteredSegments,
            _ => throw new StructuralBranchPolicyException($"Track analysis.{type} must be an array.")
        };
    }

    private static int PositiveModulo(int value, int divisor)
    {
        return (value % divisor + divisor) % divisor;
    }

    private static int ToNonNegativeInteger(int value, int fallback)
    {
        return value >= 0 ? value : fallback;
    }

    private static double ToFiniteNumber(double value, double fallback)
    {
        return double.IsFinite(value) ? value : fallback;
    }
}
