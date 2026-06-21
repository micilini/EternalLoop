using EternalLoop.AnalysisEngine.Core.BeatTracking.Agreement;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Alignment;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Candidates;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Correction;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Hybrid;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Shadow;
using EternalLoop.AnalysisEngine.Core.BeatTracking.WeakWindows;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed class BeatTrackerSelector : IBeatTracker
{
    private const string BeatThisNotConfiguredMessage = "Beat This provider is not configured yet.";

    private readonly IBeatTracker _builtInBeatTracker;
    private readonly IBeatTracker? _beatThisBeatTracker;
    private readonly BeatGridGuardrails _aiGuardrails;
    private readonly BeatGridShadowAnalyzer _shadowAnalyzer;
    private readonly BeatGridCandidateFactory _candidateFactory;
    private readonly BeatGridPhaseAlignmentAnalyzer _phaseAlignmentAnalyzer;
    private readonly BeatGridAgreementConfidenceAnalyzer _agreementConfidenceAnalyzer;
    private readonly BeatGridWeakWindowAnalyzer _weakWindowAnalyzer;
    private readonly BeatGridWeakWindowCorrectionAnalyzer _weakWindowCorrectionAnalyzer;
    private readonly BeatGridHybridSelector _hybridSelector;

    public BeatTrackerSelector(
        IBeatTracker builtInBeatTracker,
        IBeatTracker? beatThisBeatTracker = null,
        BeatGridGuardrails? aiGuardrails = null,
        BeatGridShadowAnalyzer? shadowAnalyzer = null,
        BeatGridCandidateFactory? candidateFactory = null,
        BeatGridPhaseAlignmentAnalyzer? phaseAlignmentAnalyzer = null,
        BeatGridAgreementConfidenceAnalyzer? agreementConfidenceAnalyzer = null,
        BeatGridWeakWindowAnalyzer? weakWindowAnalyzer = null,
        BeatGridWeakWindowCorrectionAnalyzer? weakWindowCorrectionAnalyzer = null,
        BeatGridHybridSelector? hybridSelector = null)
    {
        _builtInBeatTracker = builtInBeatTracker ?? throw new ArgumentNullException(nameof(builtInBeatTracker));
        _beatThisBeatTracker = beatThisBeatTracker;
        _aiGuardrails = aiGuardrails ?? new BeatGridGuardrails();
        _shadowAnalyzer = shadowAnalyzer ?? new BeatGridShadowAnalyzer();
        _candidateFactory = candidateFactory ?? new BeatGridCandidateFactory();
        _phaseAlignmentAnalyzer = phaseAlignmentAnalyzer ?? new BeatGridPhaseAlignmentAnalyzer();
        _agreementConfidenceAnalyzer = agreementConfidenceAnalyzer ?? new BeatGridAgreementConfidenceAnalyzer();
        _weakWindowAnalyzer = weakWindowAnalyzer ?? new BeatGridWeakWindowAnalyzer();
        _weakWindowCorrectionAnalyzer = weakWindowCorrectionAnalyzer ?? new BeatGridWeakWindowCorrectionAnalyzer();
        _hybridSelector = hybridSelector ?? new BeatGridHybridSelector();
    }

    public BeatTrackingResult Track(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(options);

        return options.BeatProvider switch
        {
            BeatTrackingProviderKind.BuiltIn => _builtInBeatTracker.Track(audio, features, options),
            BeatTrackingProviderKind.Auto => TrackAuto(audio, features, options),
            BeatTrackingProviderKind.BeatThis => TrackBeatThis(audio, features, options),
            BeatTrackingProviderKind.Shadow => TrackShadow(audio, features, options),
            BeatTrackingProviderKind.Hybrid => TrackHybrid(audio, features, options),
            _ => throw new InvalidOperationException($"Unsupported beat tracking provider: {options.BeatProvider}.")
        };
    }

    private BeatTrackingResult TrackAuto(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options)
    {
        return _builtInBeatTracker.Track(audio, features, options);
    }

    private BeatTrackingResult TrackBeatThis(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options)
    {
        return TrackBeatThisWithFallback(
            audio,
            features,
            options,
            fallbackToBuiltIn: options.AiFallbackMode == AiFallbackMode.FallbackToBuiltIn);
    }

    private BeatTrackingResult TrackBeatThisWithFallback(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options,
        bool fallbackToBuiltIn)
    {
        if (_beatThisBeatTracker is null)
        {
            return fallbackToBuiltIn
                ? TrackBuiltInFallback(audio, features, options, "beat-this-provider-not-configured")
                : throw new InvalidOperationException(BeatThisNotConfiguredMessage);
        }

        try
        {
            var aiResult = _beatThisBeatTracker.Track(audio, features, options);
            var guardrailResult = _aiGuardrails.Validate(aiResult, audio);

            if (guardrailResult.IsValid)
            {
                return aiResult;
            }

            var reason = $"beat-this-guardrail-rejected:{guardrailResult.Reason}";

            return fallbackToBuiltIn
                ? TrackBuiltInFallback(audio, features, options, reason)
                : throw new InvalidOperationException($"Beat This result rejected by guardrails: {guardrailResult.Reason}");
        }
        catch (Exception exception) when (fallbackToBuiltIn)
        {
            return TrackBuiltInFallback(
                audio,
                features,
                options,
                $"beat-this-provider-failed:{exception.Message}");
        }
    }

    private BeatTrackingResult TrackShadow(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options)
    {
        var profileAnalyzers = ResolveHybridProfileAnalyzers(options);
        var legacyResult = _builtInBeatTracker.Track(audio, features, options);

        if (_beatThisBeatTracker is null)
        {
            var phaseAlignment = BeatGridPhaseAlignmentDiagnostics.NotAvailable("advisor-not-available");
            var candidateSet = _candidateFactory.CreateShadowSet(
                legacyResult,
                advisor: null,
                advisorRejectionReason: "beat-this-advisor-not-configured",
                advisorAvailable: false,
                phaseAlignment: phaseAlignment);
            candidateSet = candidateSet.WithAgreementConfidence(
                BeatGridAgreementConfidenceDiagnostics.NotAvailable("advisor-not-available"));
            candidateSet = candidateSet.WithWeakWindows(
                BeatGridWeakWindowDiagnostics.NotAvailable("advisor-not-available"));
            candidateSet = candidateSet.WithWeakWindowCorrection(CreateNotAvailableCorrection("advisor-not-available"));

            return CopyWithShadowDiagnostics(
                legacyResult,
                BeatGridShadowDiagnostics.NotConfigured(legacyResult, candidateSet),
                candidateSet);
        }

        try
        {
            var advisorResult = _beatThisBeatTracker.Track(audio, features, options);
            var comparison = _shadowAnalyzer.Compare(legacyResult, advisorResult);
            var guardrailResult = _aiGuardrails.Validate(advisorResult, audio);
            var advisorRejectionReason = guardrailResult.IsValid
                ? null
                : $"beat-this-guardrail-rejected:{guardrailResult.Reason}";
            var phaseAlignment = AnalyzePhaseAlignment(legacyResult, advisorResult, advisorRejectionReason);
            var candidateSet = _candidateFactory.CreateShadowSet(
                legacyResult,
                advisorResult,
                advisorRejectionReason,
                advisorAvailable: true,
                phaseAlignment);
            candidateSet = candidateSet.WithAgreementConfidence(_agreementConfidenceAnalyzer.Analyze(candidateSet));
            candidateSet = candidateSet.WithWeakWindows(profileAnalyzers.WeakWindowAnalyzer.Analyze(candidateSet));
            candidateSet = candidateSet.WithWeakWindowCorrection(profileAnalyzers.CorrectionAnalyzer.Analyze(candidateSet));

            var diagnostics = guardrailResult.IsValid
                ? BeatGridShadowDiagnostics.Succeeded(legacyResult, advisorResult, comparison, candidateSet)
                : BeatGridShadowDiagnostics.Rejected(
                    legacyResult,
                    advisorResult,
                    advisorRejectionReason!,
                    comparison,
                    candidateSet);

            return CopyWithShadowDiagnostics(legacyResult, diagnostics, candidateSet);
        }
        catch (Exception exception)
        {
            var failureReason = $"beat-this-provider-failed:{exception.Message}";
            var phaseAlignment = BeatGridPhaseAlignmentDiagnostics.NotAvailable(failureReason);
            var candidateSet = _candidateFactory.CreateShadowSet(
                legacyResult,
                advisor: null,
                advisorRejectionReason: failureReason,
                advisorAvailable: false,
                phaseAlignment: phaseAlignment);
            candidateSet = candidateSet.WithAgreementConfidence(
                BeatGridAgreementConfidenceDiagnostics.NotAvailable(failureReason));
            candidateSet = candidateSet.WithWeakWindows(
                BeatGridWeakWindowDiagnostics.NotAvailable(failureReason));
            candidateSet = candidateSet.WithWeakWindowCorrection(CreateNotAvailableCorrection(failureReason));

            return CopyWithShadowDiagnostics(
                legacyResult,
                BeatGridShadowDiagnostics.Failed(
                    legacyResult,
                    failureReason,
                    candidateSet),
                candidateSet);
        }
    }

    private BeatTrackingResult TrackHybrid(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options)
    {
        var profileAnalyzers = ResolveHybridProfileAnalyzers(options);
        var legacyResult = _builtInBeatTracker.Track(audio, features, options);

        if (_beatThisBeatTracker is null)
        {
            var candidateSet = CreateCandidateSetWithoutAdvisor(
                legacyResult,
                "beat-this-advisor-not-configured",
                profileAnalyzers.HybridSelector);
            return CopyFromHybridSelectedCandidate(
                legacyResult,
                candidateSet.Legacy!,
                candidateSet,
                candidateSet.HybridSelection!,
                advisorAvailable: false,
                advisorWarnings: [],
                downbeatSanitized: false);
        }

        try
        {
            var advisorResult = _beatThisBeatTracker.Track(audio, features, options);
            var guardrailResult = _aiGuardrails.Validate(advisorResult, audio);
            var advisorRejectionReason = guardrailResult.IsValid
                ? null
                : $"beat-this-guardrail-rejected:{guardrailResult.Reason}";
            var candidateSet = CreateCandidateSetWithAdvisor(
                legacyResult,
                advisorResult,
                advisorRejectionReason,
                advisorAvailable: true,
                profileAnalyzers.WeakWindowAnalyzer,
                profileAnalyzers.CorrectionAnalyzer);
            var (selected, hybridDiagnostics) = profileAnalyzers.HybridSelector.SelectExplicitHybrid(candidateSet);
            candidateSet = candidateSet.WithHybridSelection(selected, hybridDiagnostics);

            return CopyFromHybridSelectedCandidate(
                legacyResult,
                selected,
                candidateSet,
                hybridDiagnostics,
                advisorAvailable: true,
                advisorWarnings: advisorResult.ProviderWarnings,
                downbeatSanitized: advisorResult.DownbeatSanitized);
        }
        catch (Exception exception)
        {
            var candidateSet = CreateCandidateSetWithoutAdvisor(
                legacyResult,
                $"beat-this-provider-failed:{exception.Message}",
                profileAnalyzers.HybridSelector);
            return CopyFromHybridSelectedCandidate(
                legacyResult,
                candidateSet.Legacy!,
                candidateSet,
                candidateSet.HybridSelection!,
                advisorAvailable: false,
                advisorWarnings: [],
                downbeatSanitized: false);
        }
    }

    private BeatGridCandidateSet CreateCandidateSetWithoutAdvisor(
        BeatTrackingResult legacyResult,
        string reason,
        BeatGridHybridSelector hybridSelector)
    {
        var phaseAlignment = BeatGridPhaseAlignmentDiagnostics.NotAvailable(reason);
        var candidateSet = _candidateFactory.CreateShadowSet(
            legacyResult,
            advisor: null,
            advisorRejectionReason: reason,
            advisorAvailable: false,
            phaseAlignment: phaseAlignment);
        candidateSet = candidateSet.WithAgreementConfidence(BeatGridAgreementConfidenceDiagnostics.NotAvailable(reason));
        candidateSet = candidateSet.WithWeakWindows(BeatGridWeakWindowDiagnostics.NotAvailable(reason));
        candidateSet = candidateSet.WithWeakWindowCorrection(CreateNotAvailableCorrection(reason));
        var (selected, hybridDiagnostics) = hybridSelector.SelectExplicitHybrid(candidateSet);
        return candidateSet.WithHybridSelection(selected, hybridDiagnostics);
    }

    private BeatGridCandidateSet CreateCandidateSetWithAdvisor(
        BeatTrackingResult legacyResult,
        BeatTrackingResult advisorResult,
        string? advisorRejectionReason,
        bool advisorAvailable,
        BeatGridWeakWindowAnalyzer? weakWindowAnalyzer = null,
        BeatGridWeakWindowCorrectionAnalyzer? correctionAnalyzer = null)
    {
        weakWindowAnalyzer ??= _weakWindowAnalyzer;
        correctionAnalyzer ??= _weakWindowCorrectionAnalyzer;
        var phaseAlignment = AnalyzePhaseAlignment(legacyResult, advisorResult, advisorRejectionReason);
        var candidateSet = _candidateFactory.CreateShadowSet(
            legacyResult,
            advisorResult,
            advisorRejectionReason,
            advisorAvailable,
            phaseAlignment);
        candidateSet = candidateSet.WithAgreementConfidence(_agreementConfidenceAnalyzer.Analyze(candidateSet));
        candidateSet = candidateSet.WithWeakWindows(weakWindowAnalyzer.Analyze(candidateSet));
        return candidateSet.WithWeakWindowCorrection(correctionAnalyzer.Analyze(candidateSet));
    }

    private (
        BeatGridWeakWindowAnalyzer WeakWindowAnalyzer,
        BeatGridWeakWindowCorrectionAnalyzer CorrectionAnalyzer,
        BeatGridHybridSelector HybridSelector) ResolveHybridProfileAnalyzers(BeatTrackingOptions options)
    {
        if (options.HybridCalibrationProfile == HybridCalibrationProfile.StrictProduction)
        {
            return (_weakWindowAnalyzer, _weakWindowCorrectionAnalyzer, _hybridSelector);
        }

        var weakOptions = BeatGridHybridCalibrationProfileFactory.CreateWeakWindowOptions(options.HybridCalibrationProfile);
        var correctionOptions = BeatGridHybridCalibrationProfileFactory.CreateCorrectionOptions(options.HybridCalibrationProfile);
        var hybridOptions = BeatGridHybridCalibrationProfileFactory.CreateHybridSelectionOptions(options.HybridCalibrationProfile);

        return (
            new BeatGridWeakWindowAnalyzer(weakOptions),
            new BeatGridWeakWindowCorrectionAnalyzer(correctionOptions),
            new BeatGridHybridSelector(hybridOptions));
    }

    private static BeatGridWeakWindowCorrectionResult CreateNotAvailableCorrection(string reason)
    {
        return new BeatGridWeakWindowCorrectionResult
        {
            CorrectedCandidate = null,
            Plan = new BeatGridWeakWindowCorrectionPlan
            {
                Enabled = true,
                Mode = BeatGridWeakWindowCorrectionMode.DiagnosticsOnly,
                Notes = [reason]
            },
            Diagnostics = BeatGridWeakWindowCorrectionDiagnostics.NotAvailable(reason)
        };
    }

    private BeatGridPhaseAlignmentDiagnostics AnalyzePhaseAlignment(
        BeatTrackingResult legacyResult,
        BeatTrackingResult advisorResult,
        string? advisorRejectionReason)
    {
        var legacyCandidate = _candidateFactory.FromResult(
            legacyResult,
            BeatGridCandidateSourceKind.LegacyBuiltIn,
            BeatGridCandidateRole.SafeAuthority,
            "legacy",
            notes: ["Shadow mode uses the primary grid as final output."]);
        var advisorCandidate = _candidateFactory.FromResult(
            advisorResult,
            BeatGridCandidateSourceKind.BeatThisAdvisor,
            BeatGridCandidateRole.Advisor,
            "beat-this-advisor",
            advisorRejectionReason,
            ["Advisor candidate is diagnostic only."]);

        return _phaseAlignmentAnalyzer.Analyze(legacyCandidate, advisorCandidate);
    }

    private BeatTrackingResult TrackBuiltInFallback(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options,
        string reason)
    {
        var builtInResult = _builtInBeatTracker.Track(audio, features, options);

        return CopyWithFallbackMetadata(builtInResult, reason);
    }

    private static BeatTrackingResult CopyWithFallbackMetadata(
        BeatTrackingResult result,
        string reason)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = result.EstimatedBpm,
            BeatTimes = result.BeatTimes.ToArray(),
            Confidences = result.Confidences.ToArray(),
            DownbeatTimes = result.DownbeatTimes.ToArray(),
            ProviderWarnings = result.ProviderWarnings,
            DownbeatSanitized = result.DownbeatSanitized,
            BeatNumbers = result.BeatNumbers.ToArray(),
            EstimatedMeter = result.EstimatedMeter,
            ProviderName = "built-in",
            ProviderVersion = "analysisengine-built-in",
            ProviderLicense = "MIT",
            ModelName = "none",
            ModelSha256 = "none",
            UsedAiProvider = false,
            UsedBuiltInProvider = true,
            UsedFallbackProvider = true,
            UsedHybridProvider = false,
            FallbackReason = reason,
            BeatGridMode = string.IsNullOrWhiteSpace(result.BeatGridMode)
                ? "built-in-fallback"
                : $"{result.BeatGridMode}+fallback",
            BeatProviderOutputMode = result.BeatProviderOutputMode,
            BeatProviderChunkCount = result.BeatProviderChunkCount,
            BeatProviderValidFrameCount = result.BeatProviderValidFrameCount,
            BeatProviderCoverageSeconds = result.BeatProviderCoverageSeconds,
            BeatProviderCoverageRatio = result.BeatProviderCoverageRatio,
            BeatActivationSummary = result.BeatActivationSummary,
            DownbeatActivationSummary = result.DownbeatActivationSummary,
            TempoCandidates = result.TempoCandidates,
            ForcedTempoBpm = result.ForcedTempoBpm,
            ElasticRefinement = result.ElasticRefinement,
            PiecewiseRefinement = result.PiecewiseRefinement,
            CompositeDpTracking = result.CompositeDpTracking,
            BeatEvidenceWeights = result.BeatEvidenceWeights,
            BeatEvidenceMean = result.BeatEvidenceMean,
            BeatEvidenceVariance = result.BeatEvidenceVariance,
            HpssRequested = result.HpssRequested,
            HpssApplied = result.HpssApplied,
            HpssMode = result.HpssMode,
            HpssAcceptedByGuardrails = result.HpssAcceptedByGuardrails,
            HpssRejectionReason = result.HpssRejectionReason,
            BeatEvidenceSource = result.BeatEvidenceSource,
            TempoCandidateSources = result.TempoCandidateSources.ToArray(),
            SelectedTempoSource = result.SelectedTempoSource,
            ShadowDiagnostics = result.ShadowDiagnostics,
            CandidateSet = result.CandidateSet
        };
    }

    private static BeatTrackingResult CopyWithShadowDiagnostics(
        BeatTrackingResult result,
        BeatGridShadowDiagnostics diagnostics,
        BeatGridCandidateSet candidateSet)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = result.EstimatedBpm,
            BeatTimes = result.BeatTimes.ToArray(),
            Confidences = result.Confidences.ToArray(),
            DownbeatTimes = result.DownbeatTimes.ToArray(),
            ProviderWarnings = result.ProviderWarnings,
            DownbeatSanitized = result.DownbeatSanitized,
            BeatNumbers = result.BeatNumbers.ToArray(),
            EstimatedMeter = result.EstimatedMeter,
            ProviderName = "built-in",
            ProviderVersion = "analysisengine-built-in",
            ProviderLicense = "MIT",
            ModelName = "none",
            ModelSha256 = "none",
            UsedAiProvider = false,
            UsedBuiltInProvider = true,
            UsedFallbackProvider = false,
            UsedHybridProvider = false,
            FallbackReason = null,
            BeatGridMode = string.IsNullOrWhiteSpace(result.BeatGridMode)
                ? "built-in-shadow"
                : $"{result.BeatGridMode}+shadow",
            BeatProviderOutputMode = result.BeatProviderOutputMode,
            BeatProviderChunkCount = result.BeatProviderChunkCount,
            BeatProviderValidFrameCount = result.BeatProviderValidFrameCount,
            BeatProviderCoverageSeconds = result.BeatProviderCoverageSeconds,
            BeatProviderCoverageRatio = result.BeatProviderCoverageRatio,
            BeatActivationSummary = result.BeatActivationSummary,
            DownbeatActivationSummary = result.DownbeatActivationSummary,
            TempoCandidates = result.TempoCandidates,
            ForcedTempoBpm = result.ForcedTempoBpm,
            ElasticRefinement = result.ElasticRefinement,
            PiecewiseRefinement = result.PiecewiseRefinement,
            CompositeDpTracking = result.CompositeDpTracking,
            BeatEvidenceWeights = result.BeatEvidenceWeights,
            BeatEvidenceMean = result.BeatEvidenceMean,
            BeatEvidenceVariance = result.BeatEvidenceVariance,
            HpssRequested = result.HpssRequested,
            HpssApplied = result.HpssApplied,
            HpssMode = result.HpssMode,
            HpssAcceptedByGuardrails = result.HpssAcceptedByGuardrails,
            HpssRejectionReason = result.HpssRejectionReason,
            BeatEvidenceSource = result.BeatEvidenceSource,
            TempoCandidateSources = result.TempoCandidateSources.ToArray(),
            SelectedTempoSource = result.SelectedTempoSource,
            ShadowDiagnostics = diagnostics,
            CandidateSet = candidateSet
        };
    }

    private static BeatTrackingResult CopyFromHybridSelectedCandidate(
        BeatTrackingResult legacyResult,
        BeatGridCandidate selected,
        BeatGridCandidateSet candidateSet,
        BeatGridHybridSelectionDiagnostics hybridDiagnostics,
        bool advisorAvailable,
        IReadOnlyList<string> advisorWarnings,
        bool downbeatSanitized)
    {
        var selectedCorrected = string.Equals(hybridDiagnostics.FinalOutputSource, "corrected-experimental", StringComparison.OrdinalIgnoreCase);
        var beatTimes = selectedCorrected ? selected.BeatTimes : legacyResult.BeatTimes;
        var confidences = selectedCorrected ? selected.Confidences : legacyResult.Confidences;
        var downbeats = selectedCorrected ? selected.DownbeatTimes : legacyResult.DownbeatTimes;

        return new BeatTrackingResult
        {
            EstimatedBpm = selectedCorrected ? selected.EstimatedBpm ?? legacyResult.EstimatedBpm : legacyResult.EstimatedBpm,
            BeatTimes = beatTimes.ToArray(),
            Confidences = confidences.ToArray(),
            DownbeatTimes = downbeats.ToArray(),
            ProviderWarnings = selectedCorrected ? advisorWarnings : [],
            DownbeatSanitized = selectedCorrected && downbeatSanitized,
            BeatNumbers = selectedCorrected ? [] : legacyResult.BeatNumbers.ToArray(),
            EstimatedMeter = selectedCorrected ? null : legacyResult.EstimatedMeter,
            ProviderName = "hybrid",
            ProviderVersion = "analysisengine-hybrid",
            ProviderLicense = legacyResult.ProviderLicense,
            ModelName = legacyResult.ModelName,
            ModelSha256 = legacyResult.ModelSha256,
            UsedAiProvider = advisorAvailable,
            UsedBuiltInProvider = true,
            UsedFallbackProvider = !selectedCorrected,
            UsedHybridProvider = true,
            FallbackReason = selectedCorrected
                ? null
                : hybridDiagnostics.SafetyRejectionReason ?? "hybrid-corrected-candidate-not-available",
            BeatGridMode = selectedCorrected ? "hybrid-experimental" : "hybrid-fallback-legacy",
            BeatProviderOutputMode = legacyResult.BeatProviderOutputMode,
            BeatProviderChunkCount = legacyResult.BeatProviderChunkCount,
            BeatProviderValidFrameCount = legacyResult.BeatProviderValidFrameCount,
            BeatProviderCoverageSeconds = legacyResult.BeatProviderCoverageSeconds,
            BeatProviderCoverageRatio = legacyResult.BeatProviderCoverageRatio,
            BeatActivationSummary = legacyResult.BeatActivationSummary,
            DownbeatActivationSummary = legacyResult.DownbeatActivationSummary,
            TempoCandidates = legacyResult.TempoCandidates,
            ForcedTempoBpm = legacyResult.ForcedTempoBpm,
            ElasticRefinement = legacyResult.ElasticRefinement,
            PiecewiseRefinement = legacyResult.PiecewiseRefinement,
            CompositeDpTracking = legacyResult.CompositeDpTracking,
            BeatEvidenceWeights = legacyResult.BeatEvidenceWeights,
            BeatEvidenceMean = legacyResult.BeatEvidenceMean,
            BeatEvidenceVariance = legacyResult.BeatEvidenceVariance,
            HpssRequested = legacyResult.HpssRequested,
            HpssApplied = legacyResult.HpssApplied,
            HpssMode = legacyResult.HpssMode,
            HpssAcceptedByGuardrails = legacyResult.HpssAcceptedByGuardrails,
            HpssRejectionReason = legacyResult.HpssRejectionReason,
            BeatEvidenceSource = legacyResult.BeatEvidenceSource,
            TempoCandidateSources = legacyResult.TempoCandidateSources.ToArray(),
            SelectedTempoSource = legacyResult.SelectedTempoSource,
            ShadowDiagnostics = null,
            CandidateSet = candidateSet
        };
    }
}
