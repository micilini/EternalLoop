using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridSafetyGateTests
{
    [Fact]
    public void Validate_rejects_missing_corrected_candidate()
    {
        new BeatGridHybridSafetyGate().Validate(CreateSet(includeCorrected: false)).Reason.Should().Be("corrected-experimental-candidate-missing");
    }

    [Fact]
    public void Validate_rejects_dense_corrected_candidate()
    {
        var result = new BeatGridHybridSafetyGate().Validate(CreateSet(corrected: CreateCandidate("corrected", [0.0, 0.1, 0.2])));

        result.IsSafe.Should().BeFalse();
        result.Reason.Should().Be("corrected-candidate-dense-grid");
    }

    [Fact]
    public void Validate_rejects_implausible_corrected_candidate()
    {
        var corrected = CreateCandidate("corrected", [], isPlausible: false);

        new BeatGridHybridSafetyGate().Validate(CreateSet(corrected: corrected)).Reason.Should().Be("beat-count-zero");
    }

    [Fact]
    public void Validate_rejects_bad_count_ratio()
    {
        var corrected = CreateCandidate("corrected", Enumerable.Range(0, 20).Select(index => index * 0.5).ToArray());

        new BeatGridHybridSafetyGate().Validate(CreateSet(corrected: corrected)).Reason.Should().Be("corrected-vs-legacy-count-ratio-out-of-range");
    }

    [Fact]
    public void Validate_rejects_no_accepted_windows()
    {
        var set = CreateSet(acceptedWindowCount: 0);

        new BeatGridHybridSafetyGate().Validate(set).Reason.Should().Be("accepted-correction-window-count-too-low");
    }

    [Fact]
    public void Validate_accepts_safe_corrected_candidate()
    {
        var result = new BeatGridHybridSafetyGate().Validate(CreateSet());

        result.IsSafe.Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_when_hbg07_flags_are_unsafe()
    {
        var set = CreateSet(shouldApplyCorrection: true);

        new BeatGridHybridSafetyGate().Validate(set).Reason.Should().Be("hbg07-should-apply-correction-unsafe");
    }

    internal static BeatGridCandidateSet CreateSet(
        BeatGridCandidate? corrected = null,
        bool includeCorrected = true,
        int acceptedWindowCount = 1,
        bool shouldApplyCorrection = false)
    {
        var legacy = CreateCandidate("legacy", [0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5], BeatGridCandidateSourceKind.LegacyBuiltIn, BeatGridCandidateRole.SafeAuthority);
        corrected = includeCorrected
            ? corrected ?? CreateCandidate("weak-window-corrected-experimental", [0.0, 0.5, 1.0, 1.48, 2.0, 2.5, 3.0, 3.5])
            : null;

        return new BeatGridCandidateSet
        {
            Legacy = legacy,
            CorrectedExperimental = corrected,
            Selected = legacy,
            All = corrected is null ? [legacy] : [legacy, corrected],
            Diagnostics = new BeatGridCandidateDiagnostics
            {
                Enabled = true,
                CandidateCount = corrected is null ? 1 : 2,
                SelectedCandidateId = "legacy",
                SelectedSource = BeatGridCandidateSourceKind.LegacyBuiltIn,
                LegacyCandidateId = "legacy"
            },
            WeakWindowCorrections = new BeatGridWeakWindowCorrectionDiagnostics
            {
                Enabled = true,
                Status = corrected is null ? "not-available" : "candidate-created",
                Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
                CorrectedCandidateCreated = corrected is not null,
                CorrectedCandidateId = corrected?.Id,
                AcceptedWindowCount = acceptedWindowCount,
                LegacyBeatCount = legacy.BeatTimes.Length,
                CorrectedBeatCount = corrected?.BeatTimes.Length ?? 0,
                ShouldApplyCorrection = shouldApplyCorrection,
                ExternalBenchmarkClaimStatus = "not-evaluated"
            },
            WeakWindowCorrectionPlan = new BeatGridWeakWindowCorrectionPlan
            {
                Enabled = true,
                Mode = BeatGridWeakWindowCorrectionMode.ExperimentalCandidate,
                AcceptedWindowCount = acceptedWindowCount
            }
        };
    }

    internal static BeatGridCandidate CreateCandidate(
        string id,
        double[] beats,
        BeatGridCandidateSourceKind source = BeatGridCandidateSourceKind.WeakWindowCorrectedExperimental,
        BeatGridCandidateRole role = BeatGridCandidateRole.CorrectedExperimental,
        bool isPlausible = true)
    {
        var median = beats.Length > 1 ? beats.Zip(beats.Skip(1), (left, right) => right - left).Where(value => value > 0.0).Order().ElementAt((beats.Length - 1) / 2) : (double?)null;
        var bpm = median > 0.0 ? 60.0 / median : (double?)null;
        var density = beats.Length > 1 && beats[^1] > beats[0] ? beats.Length / (beats[^1] - beats[0]) : (double?)null;
        var dense = density > 4.0 || bpm > 200.0 || median < 0.25;

        return new BeatGridCandidate
        {
            Id = id,
            Source = source,
            Role = role,
            ProviderName = source == BeatGridCandidateSourceKind.LegacyBuiltIn ? "built-in" : "hybrid-experimental",
            BeatGridMode = source == BeatGridCandidateSourceKind.LegacyBuiltIn ? "built-in" : "weak-window-correction-experimental",
            BeatTimes = beats,
            Confidences = Enumerable.Repeat(0.9, beats.Length).ToArray(),
            EstimatedBpm = bpm,
            Quality = new BeatGridCandidateQuality
            {
                BeatCount = beats.Length,
                EstimatedBpm = bpm,
                MedianIntervalSeconds = median,
                BeatDensityPerSecond = density,
                IsDenseGrid = dense,
                IsPlausible = isPlausible && beats.Length > 0 && !dense,
                RejectionReason = isPlausible && beats.Length > 0 && !dense ? null : "beat-count-zero"
            }
        };
    }
}
