using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;

public sealed class BeatGridCandidateFactory
{
    private const double MaxBeatDensityPerSecond = 4.0;
    private const double MaxBpm = 200.0;
    private const double MinMedianIntervalSeconds = 0.25;

    public BeatGridCandidate FromResult(
        BeatTrackingResult result,
        BeatGridCandidateSourceKind source,
        BeatGridCandidateRole role,
        string id,
        string? rejectionReason = null,
        IReadOnlyList<string>? notes = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return new BeatGridCandidate
        {
            Id = id,
            Source = source,
            Role = role,
            ProviderName = result.ProviderName,
            BeatGridMode = result.BeatGridMode,
            BeatTimes = result.BeatTimes.ToArray(),
            DownbeatTimes = result.DownbeatTimes.ToArray(),
            Confidences = result.Confidences.ToArray(),
            EstimatedBpm = double.IsFinite(result.EstimatedBpm) ? result.EstimatedBpm : null,
            Quality = CalculateQuality(result, rejectionReason, notes),
            Notes = notes ?? []
        };
    }

    public BeatGridCandidate FromBeatTimes(
        double[] beatTimes,
        double[] downbeatTimes,
        double[] confidences,
        double? estimatedBpm,
        BeatGridCandidateSourceKind source,
        BeatGridCandidateRole role,
        string id,
        string providerName,
        string beatGridMode,
        string? rejectionReason = null,
        IReadOnlyList<string>? notes = null)
    {
        ArgumentNullException.ThrowIfNull(beatTimes);
        ArgumentNullException.ThrowIfNull(downbeatTimes);
        ArgumentNullException.ThrowIfNull(confidences);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var result = new BeatTrackingResult
        {
            EstimatedBpm = estimatedBpm ?? double.NaN,
            BeatTimes = beatTimes.ToArray(),
            DownbeatTimes = downbeatTimes.ToArray(),
            Confidences = confidences.ToArray(),
            ProviderName = providerName,
            BeatGridMode = beatGridMode
        };

        return FromResult(result, source, role, id, rejectionReason, notes);
    }

    public BeatGridCandidateSet CreateShadowSet(
        BeatTrackingResult legacy,
        BeatTrackingResult? advisor,
        string? advisorRejectionReason = null,
        bool advisorAvailable = false,
        BeatGridPhaseAlignmentDiagnostics? phaseAlignment = null)
    {
        ArgumentNullException.ThrowIfNull(legacy);

        var legacyCandidate = FromResult(
            legacy,
            BeatGridCandidateSourceKind.LegacyBuiltIn,
            BeatGridCandidateRole.SafeAuthority,
            "legacy",
            notes: ["Primary candidate is the safe final authority."]);
        var advisorCandidate = advisor is null
            ? null
            : FromResult(
                advisor,
                BeatGridCandidateSourceKind.BeatThisAdvisor,
                BeatGridCandidateRole.Advisor,
                "beat-this-advisor",
                advisorRejectionReason,
                ["Advisor candidate is diagnostic only."]);
        var candidates = advisorCandidate is null
            ? [legacyCandidate]
            : new[] { legacyCandidate, advisorCandidate };

        return new BeatGridCandidateSet
        {
            Legacy = legacyCandidate,
            Advisor = advisorCandidate,
            Selected = legacyCandidate,
            All = candidates,
            Diagnostics = new BeatGridCandidateDiagnostics
            {
                Enabled = true,
                CandidateCount = candidates.Length,
                SelectedCandidateId = legacyCandidate.Id,
                SelectedSource = BeatGridCandidateSourceKind.LegacyBuiltIn,
                SelectionReason = "shadow-mode-selects-primary",
                LegacyCandidateId = legacyCandidate.Id,
                AdvisorCandidateId = advisorCandidate?.Id,
                AdvisorAvailable = advisorAvailable,
                AdvisorAcceptedAsCandidate = advisorCandidate is not null && advisorRejectionReason is null,
                AdvisorRejectionReason = advisorRejectionReason,
                Notes = ["Candidate selection is diagnostic only; final output remains Legacy/BuiltIn."]
            },
            PhaseAlignment = phaseAlignment
        };
    }

    private static BeatGridCandidateQuality CalculateQuality(
        BeatTrackingResult result,
        string? rejectionReason,
        IReadOnlyList<string>? notes)
    {
        var medianInterval = CalculateMedianInterval(result.BeatTimes);
        var durationSeconds = EstimateDurationSeconds(result.BeatTimes);
        var density = durationSeconds > 0.0 && result.BeatTimes.Length > 0
            ? result.BeatTimes.Length / durationSeconds
            : (double?)null;
        var bpm = double.IsFinite(result.EstimatedBpm) ? result.EstimatedBpm : (double?)null;
        var isDense = density > MaxBeatDensityPerSecond
            || bpm > MaxBpm
            || medianInterval < MinMedianIntervalSeconds;
        var hasBeats = result.BeatTimes.Length > 0;
        var isPlausible = hasBeats && !isDense && rejectionReason is null;

        return new BeatGridCandidateQuality
        {
            BeatCount = result.BeatTimes.Length,
            DownbeatCount = result.DownbeatTimes.Length,
            EstimatedBpm = bpm,
            MedianIntervalSeconds = medianInterval,
            BeatDensityPerSecond = density,
            IsDenseGrid = isDense,
            IsPlausible = isPlausible,
            RejectionReason = rejectionReason ?? ResolveQualityRejectionReason(hasBeats, isDense, bpm, density, medianInterval),
            Notes = notes ?? []
        };
    }

    private static string? ResolveQualityRejectionReason(
        bool hasBeats,
        bool isDense,
        double? bpm,
        double? density,
        double? medianInterval)
    {
        if (!hasBeats)
        {
            return "beat-count-zero";
        }

        if (!isDense)
        {
            return null;
        }

        if (bpm > MaxBpm)
        {
            return $"bpm-too-high:{bpm:0.###}";
        }

        if (density > MaxBeatDensityPerSecond)
        {
            return $"beat-density-too-high:{density:0.###}";
        }

        if (medianInterval < MinMedianIntervalSeconds)
        {
            return $"median-interval-too-low:{medianInterval:0.###}";
        }

        return "dense-grid";
    }

    private static double? CalculateMedianInterval(double[] beatTimes)
    {
        if (beatTimes.Length < 2)
        {
            return null;
        }

        var intervals = beatTimes
            .Zip(beatTimes.Skip(1), (left, right) => right - left)
            .Where(interval => interval > 0.0 && double.IsFinite(interval))
            .Order()
            .ToArray();

        if (intervals.Length == 0)
        {
            return null;
        }

        return intervals.Length % 2 == 1
            ? intervals[intervals.Length / 2]
            : (intervals[(intervals.Length / 2) - 1] + intervals[intervals.Length / 2]) / 2.0;
    }

    private static double EstimateDurationSeconds(double[] beatTimes)
    {
        return beatTimes.Length > 0 && double.IsFinite(beatTimes[^1])
            ? beatTimes[^1]
            : 0.0;
    }
}
