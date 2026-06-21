using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Core.Progress;
using EternalLoop.AnalysisEngine.Core.Validation;
using Microsoft.Extensions.Logging;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public sealed class TrackAnalysisPipeline : ITrackAnalysisPipeline
{
    private readonly IAudioLoader _audioLoader;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IBeatTracker _beatTracker;
    private readonly AnalysisSanityValidator _validator;
    private readonly ILogger<TrackAnalysisPipeline> _logger;

    public TrackAnalysisPipeline(
        IAudioLoader audioLoader,
        IFeatureExtractor featureExtractor,
        IBeatTracker beatTracker,
        AnalysisSanityValidator validator,
        ILogger<TrackAnalysisPipeline> logger)
    {
        _audioLoader = audioLoader ?? throw new ArgumentNullException(nameof(audioLoader));
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _beatTracker = beatTracker ?? throw new ArgumentNullException(nameof(beatTracker));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TrackAnalysis> AnalyzeAsync(
        string inputPath,
        AnalysisOptions options,
        IAnalysisProgressReporter progressReporter,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(progressReporter);

        _logger.LogInformation("Starting standalone track analysis for {InputPath}", inputPath);

        progressReporter.Report(AnalysisStage.LoadingAudio, 0.0, "Loading audio");
        var audio = await _audioLoader.LoadAsync(inputPath, options.TargetSampleRate, cancellationToken).ConfigureAwait(false);
        progressReporter.Report(AnalysisStage.LoadingAudio, 1.0, "Audio loaded");

        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.ExtractingFeatures, 0.0, "Extracting features");
        var useHpss = options.Tuning.UseHpss ?? options.MusicalQuality.BeatMicroSnap;
        var hpssOptions = new HpssOptions
        {
            UseHpss = useHpss,
            TimeMedianKernelFrames = options.Tuning.HpssTimeMedianKernelFrames ?? HpssOptions.DefaultTimeMedianKernelFrames,
            FrequencyMedianKernelBins = options.Tuning.HpssFrequencyMedianKernelBins ?? HpssOptions.DefaultFrequencyMedianKernelBins,
            MaskPower = options.Tuning.HpssMaskPower ?? HpssOptions.DefaultMaskPower,
            PercussiveMargin = options.Tuning.HpssPercussiveMargin ?? HpssOptions.DefaultPercussiveMargin,
            HarmonicMargin = options.Tuning.HpssHarmonicMargin ?? HpssOptions.DefaultHarmonicMargin
        };
        var features = _featureExtractor.Extract(audio, new FeatureExtractionOptions { Hpss = hpssOptions });
        progressReporter.Report(AnalysisStage.ExtractingFeatures, 1.0, "Features extracted");

        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.TrackingBeats, 0.0, "Tracking beats");
        var beatTrackingResult = _beatTracker.Track(audio, features, new BeatTrackingOptions
        {
            BeatProvider = options.BeatProvider,
            AiFallbackMode = options.AiFallbackMode,
            HybridCalibrationProfile = options.HybridCalibrationProfile,
            BeatMicroSnap = options.MusicalQuality.BeatMicroSnap,
            EvidenceConfidences = options.MusicalQuality.EvidenceConfidences,
            MinBpm = options.Tuning.MinTempo ?? BeatTrackingOptions.DefaultMinBpm,
            MaxBpm = options.Tuning.MaxTempo ?? BeatTrackingOptions.DefaultMaxBpm,
            TightnessLambda = options.Tuning.BeatDpTightness ?? BeatTrackingOptions.DefaultTightnessLambda,
            TempoCenterBpm = options.Tuning.StartBpmPriorCenter ?? BeatTrackingOptions.DefaultTempoCenterBpm,
            TempoPriorStdOctaves = options.Tuning.BpmPriorStdOctaves ?? BeatTrackingOptions.DefaultTempoPriorStdOctaves,
            HalfTimeCompetitivenessThreshold = options.Tuning.HalfTimeCompetitivenessThreshold ?? BeatTrackingOptions.DefaultHalfTimeCompetitivenessThreshold,
            BeatSnapWindowRatio = ResolveBeatSnapWindowRatio(options.Tuning.BeatSnapMaxMilliseconds),
            ForcedTempoBpm = options.Tuning.ForcedTempoBpm,
            UseGridTempoSelector = options.MusicalQuality.BeatMicroSnap,
            EnableElasticBeatGrid = options.MusicalQuality.BeatMicroSnap
            ,
            EnablePiecewiseBeatGrid = options.MusicalQuality.BeatMicroSnap,
            EnableCompositeBeatTracking = options.MusicalQuality.BeatMicroSnap,
            BeatEvidenceLogMelOnsetWeight = options.Tuning.BeatEvidenceLogMelOnsetWeight ?? BeatTrackingOptions.DefaultBeatEvidenceLogMelOnsetWeight,
            BeatEvidenceLowBandOnsetWeight = options.Tuning.BeatEvidenceLowBandOnsetWeight ?? BeatTrackingOptions.DefaultBeatEvidenceLowBandOnsetWeight,
            BeatEvidenceMidBandOnsetWeight = options.Tuning.BeatEvidenceMidBandOnsetWeight ?? BeatTrackingOptions.DefaultBeatEvidenceMidBandOnsetWeight,
            BeatEvidenceHighBandOnsetWeight = options.Tuning.BeatEvidenceHighBandOnsetWeight ?? BeatTrackingOptions.DefaultBeatEvidenceHighBandOnsetWeight,
            BeatEvidenceRmsDeltaWeight = options.Tuning.BeatEvidenceRmsDeltaWeight ?? BeatTrackingOptions.DefaultBeatEvidenceRmsDeltaWeight,
            BeatEvidenceMfccDeltaWeight = options.Tuning.BeatEvidenceMfccDeltaWeight ?? BeatTrackingOptions.DefaultBeatEvidenceMfccDeltaWeight,
            BeatEvidenceChromaDeltaWeight = options.Tuning.BeatEvidenceChromaDeltaWeight ?? BeatTrackingOptions.DefaultBeatEvidenceChromaDeltaWeight,
            BeatEvidenceNoveltyWeight = options.Tuning.BeatEvidenceNoveltyWeight ?? BeatTrackingOptions.DefaultBeatEvidenceNoveltyWeight
            ,
            UseHpss = useHpss,
            HpssMode = options.Tuning.HpssMode ?? "percussive-beat-only",
            HpssTimeMedianKernelFrames = hpssOptions.TimeMedianKernelFrames,
            HpssFrequencyMedianKernelBins = hpssOptions.FrequencyMedianKernelBins,
            HpssMaskPower = hpssOptions.MaskPower,
            HpssPercussiveMargin = hpssOptions.PercussiveMargin,
            HpssHarmonicMargin = hpssOptions.HarmonicMargin,
            FullMixOnsetWeight = options.Tuning.FullMixOnsetWeight ?? BeatTrackingOptions.DefaultFullMixOnsetWeight,
            PercussiveOnsetWeight = options.Tuning.PercussiveOnsetWeight ?? BeatTrackingOptions.DefaultPercussiveOnsetWeight,
            HarmonicOnsetWeight = options.Tuning.HarmonicOnsetWeight ?? BeatTrackingOptions.DefaultHarmonicOnsetWeight
        });
        var beats = BeatFeatureAggregator.AggregateFeatures(
            beatTrackingResult,
            features,
            audio.SampleRate,
            options.TimeSignature);
        progressReporter.Report(AnalysisStage.TrackingBeats, 1.0, $"Detected {beats.Count} beats");

        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.BuildingAnalysis, 0.0, "Building track analysis");
        var analysis = BuildAnalysis(inputPath, audio, features, beatTrackingResult, beats, options);
        progressReporter.Report(AnalysisStage.BuildingAnalysis, 1.0, "Track analysis built");

        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.Validating, 0.0, "Validating analysis");
        var validation = _validator.Validate(analysis);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
        }

        progressReporter.Report(AnalysisStage.Validating, 1.0, "Analysis validated");
        progressReporter.Report(AnalysisStage.Done, 1.0, "Analysis complete");

        _logger.LogInformation(
            "Standalone track analysis completed: {BeatCount} beats, {SegmentCount} segments",
            analysis.Beats.Count,
            analysis.Segments.Count);

        return analysis;
    }

    private static TrackAnalysis BuildAnalysis(
        string inputPath,
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingResult beatTrackingResult,
        IReadOnlyList<Beat> beats,
        AnalysisOptions options)
    {
        var onsetSource = options.MusicalQuality.AdaptiveTatums && features.OnsetEnvelope.Length == features.SpectralFlux.Length
            ? features.OnsetEnvelope
            : features.SpectralFlux;
        var odf = OnsetDetectionFunction.Build(onsetSource, BeatTrackingOptions.DefaultOdfSmoothWindow);
        var framesPerSecond = audio.SampleRate / (double)features.HopLengthSamples;
        var providerBars = BarBuilderFromDownbeats.Build(
            beats,
            beatTrackingResult.DownbeatTimes,
            options.TimeSignature);
        BarPhaseSelectionResult barPhase;
        IReadOnlyList<Bar> bars;

        if (providerBars.UsedProviderDownbeats)
        {
            barPhase = providerBars.BarPhaseSelection;
            bars = providerBars.Bars;
        }
        else
        {
            barPhase = options.MusicalQuality.BeatMicroSnap
                ? BarPhaseSelector.Select(beats, odf, framesPerSecond, options.TimeSignature)
                : new BarPhaseSelectionResult(0, [new BarPhaseCandidate(0, 1.0, 1.0, 1.0, 1.0, 1.0)], "phase-zero");
            bars = TimeQuantumBuilder.BuildBars(beats, options.TimeSignature, barPhase.SelectedPhase);
        }
        var effectiveTatumMode = ResolveEffectiveTatumMode(
            options.MusicalQuality.TatumMode,
            options.MusicalQuality.AdaptiveTatums,
            beatTrackingResult);
        var tatums = effectiveTatumMode == TatumMode.FixedTwoPerBeat
            ? TimeQuantumBuilder.BuildFixedTwoPerBeatTatums(beats)
            : TimeQuantumBuilder.BuildTatums(
                beats,
                odf,
                framesPerSecond,
                effectiveTatumMode == TatumMode.Adaptive,
                options.MusicalQuality.EvidenceConfidences);
        var sections = TimeQuantumBuilder.BuildSections(
            audio,
            bars,
            beats,
            beatTrackingResult.EstimatedBpm,
            options.TimeSignature,
            options.MusicalQuality.StructuralSections,
            options.MusicalQuality.EvidenceConfidences);
        var sectionNoveltySummary = options.MusicalQuality.StructuralSections
            ? TimeQuantumBuilder.AnalyzeSectionNovelty(audio, bars, beats, options.TimeSignature)
            : new SectionNoveltySummary(0, 0, 0.0, 0.0);
        var segmentResult = SegmentBuilder.BuildDetailed(
            features,
            audio.SampleRate,
            options.MusicalQuality.AcousticSegmentation,
            options.MusicalQuality.EvidenceConfidences);
        var diagnostics = new AnalysisDiagnostics
        {
            RequestedAcousticSegmentation = options.MusicalQuality.AcousticSegmentation,
            RequestedBeatMicroSnap = options.MusicalQuality.BeatMicroSnap,
            RequestedAdaptiveTatums = options.MusicalQuality.AdaptiveTatums,
            RequestedStructuralSections = options.MusicalQuality.StructuralSections,
            RequestedEvidenceConfidences = options.MusicalQuality.EvidenceConfidences,
            RequestedTatumMode = options.MusicalQuality.TatumMode.ToString(),
            SegmentationMode = segmentResult.Mode,
            BeatGridMode = beatTrackingResult.BeatGridMode,
            TatumMode = ResolveTatumMode(effectiveTatumMode, tatums, beats),
            SectionMode = ResolveSectionMode(audio, sections, options.MusicalQuality.StructuralSections),
            NoveltyBoundaryRatio = segmentResult.NoveltyBoundaryRatio,
            BeatStdDevRatio = CalculateStdDevRatio(beats.Select(beat => beat.Duration)),
            BeatConfidenceVariance = CalculateVariance(beats.Select(beat => beat.Confidence)),
            SegmentConfidenceVariance = CalculateVariance(segmentResult.Segments.Select(segment => segment.Confidence)),
            SectionConfidenceVariance = CalculateVariance(sections.Select(section => section.Confidence)),
            SelectedTempo = beatTrackingResult.EstimatedBpm,
            ForcedTempoBpm = beatTrackingResult.ForcedTempoBpm,
            BeatProviderName = beatTrackingResult.ProviderName,
            BeatProviderVersion = beatTrackingResult.ProviderVersion,
            BeatProviderLicense = beatTrackingResult.ProviderLicense,
            BeatProviderModelName = beatTrackingResult.ModelName,
            BeatProviderModelSha256 = beatTrackingResult.ModelSha256,
            BeatProviderUsedAi = beatTrackingResult.UsedAiProvider,
            BeatProviderUsedBuiltIn = beatTrackingResult.UsedBuiltInProvider,
            BeatProviderUsedFallback = beatTrackingResult.UsedFallbackProvider,
            BeatProviderUsedHybrid = beatTrackingResult.UsedHybridProvider,
            BeatProviderFallbackReason = beatTrackingResult.FallbackReason,
            BeatProviderWarnings = beatTrackingResult.ProviderWarnings,
            BeatProviderDownbeatSanitized = beatTrackingResult.DownbeatSanitized,
            BeatProviderDownbeatCount = beatTrackingResult.DownbeatTimes.Length,
            BeatProviderBeatNumberCount = beatTrackingResult.BeatNumbers.Length,
            BeatProviderEstimatedMeter = beatTrackingResult.EstimatedMeter,
            BeatProviderOutputMode = beatTrackingResult.BeatProviderOutputMode,
            BeatProviderChunkCount = beatTrackingResult.BeatProviderChunkCount,
            BeatProviderValidFrameCount = beatTrackingResult.BeatProviderValidFrameCount,
            BeatProviderCoverageSeconds = beatTrackingResult.BeatProviderCoverageSeconds,
            BeatProviderCoverageRatio = beatTrackingResult.BeatProviderCoverageRatio,
            BeatProviderBeatActivationSummary = beatTrackingResult.BeatActivationSummary,
            BeatProviderDownbeatActivationSummary = beatTrackingResult.DownbeatActivationSummary,
            BeatProviderShadowDiagnostics = beatTrackingResult.ShadowDiagnostics,
            BeatProviderCandidateSet = beatTrackingResult.CandidateSet,
            TempoCandidates = beatTrackingResult.TempoCandidates,
            BarPhaseMode = barPhase.Mode,
            SelectedBarPhase = barPhase.SelectedPhase,
            BarPhaseScore = barPhase.Candidates.FirstOrDefault(candidate => candidate.Phase == barPhase.SelectedPhase)?.Score ?? 0.0,
            BarPhaseCandidates = barPhase.Candidates,
            BeatDriftMode = beatTrackingResult.ElasticRefinement?.Mode ?? "none",
            BeatElasticRefinementApplied = beatTrackingResult.ElasticRefinement?.Applied ?? false,
            BeatElasticMedianShiftMs = beatTrackingResult.ElasticRefinement?.MedianShiftMs ?? 0.0,
            BeatElasticP90ShiftMs = beatTrackingResult.ElasticRefinement?.P90ShiftMs ?? 0.0,
            BeatElasticIntervalStdDevRatioBefore = beatTrackingResult.ElasticRefinement?.IntervalStdDevRatioBefore ?? 0.0,
            BeatElasticIntervalStdDevRatioAfter = beatTrackingResult.ElasticRefinement?.IntervalStdDevRatioAfter ?? 0.0,
            BeatElasticOnsetScoreBefore = beatTrackingResult.ElasticRefinement?.OnsetScoreBefore ?? 0.0,
            BeatElasticOnsetScoreAfter = beatTrackingResult.ElasticRefinement?.OnsetScoreAfter ?? 0.0,
            BeatPiecewiseRefinementApplied = beatTrackingResult.PiecewiseRefinement?.Applied ?? false,
            BeatPiecewiseMode = beatTrackingResult.PiecewiseRefinement?.Mode ?? "none",
            BeatPiecewiseWindowCount = beatTrackingResult.PiecewiseRefinement?.WindowCount ?? 0,
            BeatPiecewiseAcceptedWindows = beatTrackingResult.PiecewiseRefinement?.AcceptedWindows ?? 0,
            BeatPiecewiseMeanShiftMs = beatTrackingResult.PiecewiseRefinement?.MeanShiftMs ?? 0.0,
            BeatPiecewiseMaxShiftMs = beatTrackingResult.PiecewiseRefinement?.MaxShiftMs ?? 0.0,
            BeatPiecewiseOnsetScoreBefore = beatTrackingResult.PiecewiseRefinement?.OnsetScoreBefore ?? 0.0,
            BeatPiecewiseOnsetScoreAfter = beatTrackingResult.PiecewiseRefinement?.OnsetScoreAfter ?? 0.0,
            BeatPiecewiseRegularityBefore = beatTrackingResult.PiecewiseRefinement?.RegularityBefore ?? 0.0,
            BeatPiecewiseRegularityAfter = beatTrackingResult.PiecewiseRefinement?.RegularityAfter ?? 0.0,
            BeatEvidenceMode = beatTrackingResult.BeatEvidenceWeights.Count > 0 ? "composite" : "none",
            BeatEvidenceWeights = beatTrackingResult.BeatEvidenceWeights,
            BeatEvidenceSelectedChannel = beatTrackingResult.BeatEvidenceWeights.Count > 0 ? "composite" : "none",
            BeatEvidenceMean = beatTrackingResult.BeatEvidenceMean,
            BeatEvidenceVariance = beatTrackingResult.BeatEvidenceVariance,
            BeatCompositeDpApplied = beatTrackingResult.CompositeDpTracking?.Applied ?? false,
            BeatCompositeDpMode = beatTrackingResult.CompositeDpTracking?.Mode ?? "none",
            BeatCompositeDpEvidenceBefore = beatTrackingResult.CompositeDpTracking?.EvidenceMeanBefore ?? 0.0,
            BeatCompositeDpEvidenceAfter = beatTrackingResult.CompositeDpTracking?.EvidenceMeanAfter ?? 0.0,
            BeatCompositeDpRegularityBefore = beatTrackingResult.CompositeDpTracking?.IntervalStdDevRatioBefore ?? 0.0,
            BeatCompositeDpRegularityAfter = beatTrackingResult.CompositeDpTracking?.IntervalStdDevRatioAfter ?? 0.0,
            HpssRequested = options.Tuning.UseHpss ?? options.MusicalQuality.BeatMicroSnap,
            HpssApplied = features.HpssApplied,
            HpssMode = beatTrackingResult.HpssMode,
            HpssTimeKernelFrames = options.Tuning.HpssTimeMedianKernelFrames ?? HpssOptions.DefaultTimeMedianKernelFrames,
            HpssFrequencyKernelBins = options.Tuning.HpssFrequencyMedianKernelBins ?? HpssOptions.DefaultFrequencyMedianKernelBins,
            HpssMaskPower = options.Tuning.HpssMaskPower ?? HpssOptions.DefaultMaskPower,
            HpssPercussiveMargin = options.Tuning.HpssPercussiveMargin ?? HpssOptions.DefaultPercussiveMargin,
            HpssHarmonicMargin = options.Tuning.HpssHarmonicMargin ?? HpssOptions.DefaultHarmonicMargin,
            HpssPercussiveEnergyRatio = features.HpssPercussiveEnergyRatio,
            HpssHarmonicEnergyRatio = features.HpssHarmonicEnergyRatio,
            TempoCandidateSources = beatTrackingResult.TempoCandidateSources,
            SelectedTempoSource = beatTrackingResult.SelectedTempoSource,
            BeatEvidenceSource = beatTrackingResult.BeatEvidenceSource,
            HpssAcceptedByGuardrails = beatTrackingResult.HpssAcceptedByGuardrails,
            HpssRejectionReason = beatTrackingResult.HpssRejectionReason,
            SectionFeatureResolution = options.MusicalQuality.StructuralSections ? "bar" : "none",
            SectionCandidateCount = sectionNoveltySummary.CandidateCount,
            SectionSelectedCount = sections.Count,
            SectionNoveltyMean = sectionNoveltySummary.NoveltyMean,
            SectionNoveltyMax = sectionNoveltySummary.NoveltyMax,
            SectionScaleUsed = options.MusicalQuality.StructuralSections ? "multi-scale" : "none",
            SegmentTargetDensity = segmentResult.TargetDensity,
            SegmentActualDensity = segmentResult.ActualDensity,
            SegmentCandidateCount = segmentResult.CandidateCount,
            SegmentSelectedCount = segmentResult.SelectedCount,
            SegmentNoveltyBoundaryRatio = segmentResult.NoveltyBoundaryRatio
        };

        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = audio.FileHash,
                FilePath = inputPath,
                DurationSeconds = audio.DurationSeconds,
                SampleRate = audio.SampleRate,
                Tempo = beatTrackingResult.EstimatedBpm,
                TimeSignature = options.TimeSignature,
                SchemaVersion = options.SchemaVersion,
                AnalyzedAt = DateTime.UtcNow
            },
            Segments = segmentResult.Segments,
            Beats = beats,
            Bars = bars,
            Tatums = tatums,
            Sections = sections,
            MicroFingerprints = [],
            Ai = null,
            BeatProvider = BeatProviderExportDiagnostics.FromDiagnostics(diagnostics),
            Diagnostics = diagnostics
        };
    }

    private static double ResolveBeatSnapWindowRatio(double? beatSnapMaxMilliseconds)
    {
        if (beatSnapMaxMilliseconds is null)
        {
            return BeatTrackingOptions.DefaultBeatSnapWindowRatio;
        }

        return Math.Clamp(beatSnapMaxMilliseconds.Value / 500.0, 0.02, 0.30);
    }

    private static string ResolveSectionMode(
        LoadedAudio audio,
        IReadOnlyList<Section> sections,
        bool structuralSections)
    {
        if (!structuralSections)
        {
            return sections.Count == 1 && audio.DurationSeconds <= 60.0 ? "single-fallback" : "block-fallback";
        }

        if (sections.Count == 1)
        {
            return "single-fallback";
        }

        var durations = sections.Select(section => Math.Round(section.Duration, 3)).Distinct().Count();
        return durations > 2 ? "novelty-boundary" : "block-fallback";
    }

    private static TatumMode ResolveEffectiveTatumMode(
        TatumMode requestedMode,
        bool adaptiveTatums,
        BeatTrackingResult beatTrackingResult)
    {
        if (requestedMode == TatumMode.FixedTwoPerBeat)
        {
            return TatumMode.FixedTwoPerBeat;
        }

        if (requestedMode == TatumMode.Adaptive)
        {
            return TatumMode.Adaptive;
        }

        var aiProviderSucceeded = beatTrackingResult.UsedAiProvider
            && !beatTrackingResult.UsedFallbackProvider;

        if (aiProviderSucceeded)
        {
            return TatumMode.FixedTwoPerBeat;
        }

        return adaptiveTatums ? TatumMode.Adaptive : TatumMode.Default;
    }

    private static string ResolveTatumMode(
        TatumMode effectiveTatumMode,
        IReadOnlyList<Models.Tatum> tatums,
        IReadOnlyList<Models.Beat> beats)
    {
        if (effectiveTatumMode == TatumMode.FixedTwoPerBeat)
        {
            return "fixed-two-per-beat";
        }

        if (effectiveTatumMode == TatumMode.Adaptive
            && (tatums.Count != beats.Count * 2
                || CalculateVariance(tatums.Select(tatum => tatum.Confidence)) > 0.000001))
        {
            return "onset-subdivision";
        }

        return "uniform-fallback";
    }

    private static double CalculateStdDevRatio(IEnumerable<double> values)
    {
        var sorted = values.Where(value => value > 0.0 && double.IsFinite(value)).Order().ToArray();
        if (sorted.Length == 0)
        {
            return 0.0;
        }

        var median = sorted.Length % 2 == 1
            ? sorted[sorted.Length / 2]
            : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0;

        return median > 0.0 ? Math.Sqrt(CalculateVariance(sorted)) / median : 0.0;
    }

    private static double CalculateVariance(IEnumerable<double> values)
    {
        var array = values.Where(double.IsFinite).ToArray();
        if (array.Length == 0)
        {
            return 0.0;
        }

        var average = array.Average();
        return array.Sum(value => Math.Pow(value - average, 2.0)) / array.Length;
    }
}
