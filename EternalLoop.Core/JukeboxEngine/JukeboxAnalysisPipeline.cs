using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.BeatTracking;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Similarity;
using Microsoft.Extensions.Logging;

namespace EternalLoop.Core.JukeboxEngine;

public sealed class JukeboxAnalysisPipeline : IJukeboxAnalysisPipeline
{
    private const int AnalysisSampleRate = 22_050;
    private const int DefaultTimeSignature = 4;
    private const int MaximumAiFailureReasonLength = 120;

    private readonly IAudioLoader _audioLoader;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IBeatTracker _beatTracker;
    private readonly IBranchFinder _branchFinder;
    private readonly ITrackAnalysisCache _cache;
    private readonly IAiEmbeddingExtractor _aiEmbeddingExtractor;
    private readonly AiBeatEmbeddingAggregator _aiBeatEmbeddingAggregator;
    private readonly AiFailureDiagnosticWriter _aiFailureDiagnosticWriter;
    private readonly ILogger<JukeboxAnalysisPipeline> _logger;

    public JukeboxAnalysisPipeline(
        IAudioLoader audioLoader,
        IFeatureExtractor featureExtractor,
        IBeatTracker beatTracker,
        IBranchFinder branchFinder,
        ITrackAnalysisCache cache,
        IAiEmbeddingExtractor aiEmbeddingExtractor,
        AiBeatEmbeddingAggregator aiBeatEmbeddingAggregator,
        AiFailureDiagnosticWriter aiFailureDiagnosticWriter,
        ILogger<JukeboxAnalysisPipeline> logger)
    {
        _audioLoader = audioLoader ?? throw new ArgumentNullException(nameof(audioLoader));
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _beatTracker = beatTracker ?? throw new ArgumentNullException(nameof(beatTracker));
        _branchFinder = branchFinder ?? throw new ArgumentNullException(nameof(branchFinder));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _aiEmbeddingExtractor = aiEmbeddingExtractor ?? throw new ArgumentNullException(nameof(aiEmbeddingExtractor));
        _aiBeatEmbeddingAggregator = aiBeatEmbeddingAggregator ?? throw new ArgumentNullException(nameof(aiBeatEmbeddingAggregator));
        _aiFailureDiagnosticWriter = aiFailureDiagnosticWriter ?? throw new ArgumentNullException(nameof(aiFailureDiagnosticWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JukeboxAnalysisResult> AnalyzeAsync(
        string filePath,
        IAnalysisProgressReporter progressReporter,
        CancellationToken cancellationToken,
        bool forceReanalysis = false,
        BranchFindingOptions? branchOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(progressReporter);

        _logger.LogInformation("Starting jukebox analysis for {FilePath}", filePath);
        var effectiveBranchOptions = branchOptions ?? new BranchFindingOptions();

        progressReporter.Report(AnalysisStage.Loading, 0.0, "Loading audio");
        var audio = await _audioLoader.LoadAsync(filePath, AnalysisSampleRate, cancellationToken).ConfigureAwait(false);
        progressReporter.Report(AnalysisStage.Loading, 1.0, "Audio loaded");

        cancellationToken.ThrowIfCancellationRequested();

        if (!forceReanalysis)
        {
            var cachedAnalysis = await _cache.TryGetAsync(audio.FileHash, cancellationToken).ConfigureAwait(false);
            if (cachedAnalysis is not null)
            {
                if (BeatDensitySanityCheck.IsSuspicious(
                        audio.DurationSeconds,
                        cachedAnalysis.Metadata.Tempo,
                        cachedAnalysis.Beats.Count))
                {
                    _logger.LogWarning(
                        "Ignoring suspicious cached analysis for {FileHash}: {Reason}",
                        audio.FileHash,
                        BeatDensitySanityCheck.Describe(
                            audio.DurationSeconds,
                            cachedAnalysis.Metadata.Tempo,
                            cachedAnalysis.Beats.Count));
                }
                else if (!CanUseCachedAnalysis(cachedAnalysis, effectiveBranchOptions))
                {
                    _logger.LogInformation(
                        "Ignoring cached analysis for {FileHash}: AI data is missing or incompatible",
                        audio.FileHash);
                }
                else
                {
                    _logger.LogInformation("Track analysis cache hit for {FileHash}", audio.FileHash);

                    progressReporter.Report(AnalysisStage.BuildingGraph, 0.0, "Loading loop map");
                    var cachedGraph = BuildGraph(cachedAnalysis, effectiveBranchOptions);
                    BranchQualityDiagnosticWriter.WriteIfEnabled(cachedAnalysis, cachedGraph, effectiveBranchOptions);
                    progressReporter.Report(AnalysisStage.BuildingGraph, 1.0, "Loop map ready");
                    progressReporter.Report(AnalysisStage.Done, 1.0, "Loaded from cache");

                    return new JukeboxAnalysisResult
                    {
                        Audio = audio,
                        Analysis = cachedAnalysis,
                        Graph = cachedGraph,
                        LoadedFromCache = true,
                        AiRun = CreateCacheAiRunInfo(cachedAnalysis, effectiveBranchOptions)
                    };
                }
            }

            _logger.LogInformation("Track analysis cache miss for {FileHash}", audio.FileHash);
        }
        else
        {
            _logger.LogInformation("Force reanalysis requested for {FileHash}; bypassing cache", audio.FileHash);
            progressReporter.Report(AnalysisStage.Loading, 1.0, "Refreshing saved analysis");
        }

        progressReporter.Report(AnalysisStage.ExtractingFeatures, 0.0, "Extracting features");
        var featureOptions = new FeatureExtractionOptions();
        var features = _featureExtractor.Extract(audio, featureOptions);
        progressReporter.Report(AnalysisStage.ExtractingFeatures, 1.0, "Features extracted");

        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.TrackingBeats, 0.0, "Tracking beats");
        var beatOptions = new BeatTrackingOptions();
        var beatTracking = _beatTracker.Track(audio, features, beatOptions);
        var beats = BeatFeatureAggregator.AggregateFeatures(
            beatTracking,
            features,
            audio.SampleRate,
            timeSignature: DefaultTimeSignature);
        if (BeatDensitySanityCheck.IsSuspicious(
                audio.DurationSeconds,
                beatTracking.EstimatedBpm,
                beats.Count))
        {
            _logger.LogWarning(
                "Fresh beat tracking result is suspicious: {Reason}",
                BeatDensitySanityCheck.Describe(
                    audio.DurationSeconds,
                    beatTracking.EstimatedBpm,
                    beats.Count));
        }

        progressReporter.Report(AnalysisStage.TrackingBeats, 1.0, $"Detected {beats.Count} beats");
        var microFingerprints = ExtractMicroFingerprints(
            beats,
            features,
            audio.SampleRate,
            effectiveBranchOptions);

        cancellationToken.ThrowIfCancellationRequested();

        var aiResult = await ExtractAiDataAsync(
            filePath,
            audio,
            progressReporter,
            effectiveBranchOptions,
            beats,
            cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.BuildingGraph, 0.0, "Building graph");
        var analysis = BuildAnalysis(filePath, audio, beatTracking, features, beats, microFingerprints, aiResult.Data);
        await _cache.SaveAsync(analysis, cancellationToken).ConfigureAwait(false);
        var graph = BuildGraph(analysis, effectiveBranchOptions);
        BranchQualityDiagnosticWriter.WriteIfEnabled(analysis, graph, effectiveBranchOptions);
        progressReporter.Report(AnalysisStage.BuildingGraph, 1.0, $"Built {graph.JumpEdges.Sum(pair => pair.Value.Count)} jump edges");

        progressReporter.Report(AnalysisStage.Done, 1.0, "Analysis complete");

        _logger.LogInformation(
            "Jukebox analysis completed: {BeatCount} beats, {EdgeCount} edges",
            analysis.Beats.Count,
            graph.JumpEdges.Sum(pair => pair.Value.Count));

        return new JukeboxAnalysisResult
        {
            Audio = audio,
            Analysis = analysis,
            Graph = graph,
            LoadedFromCache = false,
            AiRun = aiResult.RunInfo
        };
    }

    public JukeboxGraph BuildGraph(
        IReadOnlyList<Beat> beats,
        BranchFindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(options);

        var edges = _branchFinder.FindBranches(beats, options);
        return JukeboxGraphBuilder.Build(beats, edges, options);
    }

    public JukeboxGraph BuildGraph(
        TrackAnalysis analysis,
        BranchFindingOptions options)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(options);

        var edges = _branchFinder.FindBranches(analysis, options);
        return JukeboxGraphBuilder.Build(analysis.Beats, edges, options);
    }

    private static TrackAnalysis BuildAnalysis(
        string filePath,
        LoadedAudio audio,
        BeatTrackingResult beatTracking,
        FeatureMatrix features,
        IReadOnlyList<Beat> beats,
        IReadOnlyList<BeatMicroFingerprint> microFingerprints,
        AiAnalysisData? aiData)
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = audio.FileHash,
                FilePath = filePath,
                DurationSeconds = audio.DurationSeconds,
                SampleRate = audio.SampleRate,
                Tempo = beatTracking.EstimatedBpm,
                TimeSignature = DefaultTimeSignature,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion,
                AnalyzedAt = DateTime.UtcNow
            },
            Segments = BuildSegments(features, audio.SampleRate),
            Beats = beats,
            Bars = BuildBars(beats, DefaultTimeSignature),
            Tatums = BuildTatums(beats),
            Sections = BuildSections(audio, beatTracking.EstimatedBpm),
            MicroFingerprints = microFingerprints,
            Ai = aiData
        };
    }

    private static IReadOnlyList<BeatMicroFingerprint> ExtractMicroFingerprints(
        IReadOnlyList<Beat> beats,
        FeatureMatrix features,
        int sampleRate,
        BranchFindingOptions options)
    {
        if (!options.UseMicrosegmentSimilarity)
        {
            return [];
        }

        var microsegmentCount = Math.Clamp(
            Math.Max(options.MicrosegmentCount, TuningDefaultValues.MicrosegmentCount),
            TuningDefaultValues.MinMicrosegmentCount,
            TuningDefaultValues.MaxMicrosegmentCount);

        return BeatMicrosegmentExtractor.Extract(beats, features, sampleRate, microsegmentCount);
    }

    private async Task<AiExtractionPipelineResult> ExtractAiDataAsync(
        string filePath,
        LoadedAudio audio,
        IAnalysisProgressReporter progressReporter,
        BranchFindingOptions branchOptions,
        IReadOnlyList<Beat> beats,
        CancellationToken cancellationToken)
    {
        if (!branchOptions.UseAiSimilarity)
        {
            return new AiExtractionPipelineResult
            {
                Data = null,
                RunInfo = AiAnalysisRunInfo.Disabled
            };
        }

        progressReporter.Report(AnalysisStage.RunningAi, 0.0, "Running local AI similarity");
        var aiOptions = CreateAiOptions(branchOptions);
        try
        {
            var extractionResult = await _aiEmbeddingExtractor.ExtractAsync(audio, progressReporter, cancellationToken).ConfigureAwait(false);
            var beatEmbeddings = _aiBeatEmbeddingAggregator.Aggregate(beats, extractionResult.Frames, aiOptions);
            progressReporter.Report(AnalysisStage.RunningAi, 1.0, "AI beat embeddings ready");

            return new AiExtractionPipelineResult
            {
                Data = new AiAnalysisData
                {
                    ModelId = extractionResult.ModelId,
                    ModelVersion = extractionResult.ModelVersion,
                    SampleRate = extractionResult.SampleRate,
                    EmbeddingDimensions = extractionResult.EmbeddingDimensions,
                    BeatEmbeddings = beatEmbeddings
                },
                RunInfo = AiAnalysisRunInfo.Completed
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsRecoverableAiException(ex))
        {
            var diagnosticFilePath = TryWriteAiFailureDiagnostic(filePath, audio, beats, branchOptions, ex);
            _logger.LogWarning(
                ex,
                "Local AI similarity failed; continuing with classic analysis. DiagnosticFilePath: {DiagnosticFilePath}. Full exception: {ExceptionText}",
                diagnosticFilePath,
                ex.ToString());
            progressReporter.Report(AnalysisStage.RunningAi, 1.0, "AI similarity failed. Using classic analysis.");
            return new AiExtractionPipelineResult
            {
                Data = null,
                RunInfo = AiAnalysisRunInfo.FailedFallback(SanitizeAiFailureReason(ex), diagnosticFilePath)
            };
        }
    }

    private string? TryWriteAiFailureDiagnostic(
        string filePath,
        LoadedAudio audio,
        IReadOnlyList<Beat> beats,
        BranchFindingOptions branchOptions,
        Exception exception)
    {
        try
        {
            return _aiFailureDiagnosticWriter.Write(filePath, audio, beats, branchOptions, exception);
        }
        catch (Exception diagnosticException)
        {
            _logger.LogError(
                diagnosticException,
                "Failed to write AI failure diagnostic report. Original AI exception: {OriginalException}",
                exception.ToString());

            return null;
        }
    }

    private static bool IsRecoverableAiException(Exception exception)
    {
        return exception is OnnxInferenceException
            or IndexOutOfRangeException
            or ArgumentException
            or InvalidOperationException;
    }

    private static AiAnalysisRunInfo CreateCacheAiRunInfo(
        TrackAnalysis cachedAnalysis,
        BranchFindingOptions options)
    {
        if (!options.UseAiSimilarity)
        {
            return AiAnalysisRunInfo.Disabled;
        }

        return cachedAnalysis.Ai is not null
            ? AiAnalysisRunInfo.LoadedFromCache
            : AiAnalysisRunInfo.Disabled;
    }

    private static string SanitizeAiFailureReason(Exception exception)
    {
        var exceptionName = exception.GetType().Name;
        var message = exception.Message;

        if (string.IsNullOrWhiteSpace(message))
        {
            return exceptionName;
        }

        var normalized = message.Replace(Environment.NewLine, " ", StringComparison.Ordinal).Trim();

        if (normalized.Length > MaximumAiFailureReasonLength)
        {
            normalized = normalized[..MaximumAiFailureReasonLength].TrimEnd() + "...";
        }

        return $"{exceptionName}: {normalized}";
    }

    private static AiAnalysisOptions CreateAiOptions(BranchFindingOptions branchOptions)
    {
        return new AiAnalysisOptions
        {
            IsEnabled = branchOptions.UseAiSimilarity,
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            RejectionThreshold = branchOptions.AiRejectionThreshold,
            PenaltyStartThreshold = branchOptions.AiPenaltyStartThreshold,
            PenaltyStrength = branchOptions.AiPenaltyStrength,
            BeatContextBefore = TuningDefaultValues.AiBeatContextBefore,
            BeatContextAfter = TuningDefaultValues.AiBeatContextAfter
        };
    }

    private static bool CanUseCachedAnalysis(TrackAnalysis analysis, BranchFindingOptions options)
    {
        if (options.UseMicrosegmentSimilarity && analysis.MicroFingerprints.Count == 0)
        {
            return false;
        }

        if (!options.UseAiSimilarity)
        {
            return true;
        }

        return analysis.Ai is not null
            && string.Equals(analysis.Ai.ModelId, AiModelDefaultValues.DiscogsEffNetModelId, StringComparison.Ordinal)
            && analysis.Ai.EmbeddingDimensions == AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions;
    }

    private sealed class AiExtractionPipelineResult
    {
        public required AiAnalysisData? Data { get; init; }

        public required AiAnalysisRunInfo RunInfo { get; init; }
    }

    private static IReadOnlyList<Segment> BuildSegments(FeatureMatrix features, int sampleRate)
    {
        var frameCount = Math.Min(features.Mfcc.Length, features.Chroma.Length);
        if (frameCount == 0)
        {
            return [];
        }

        var segments = new List<Segment>(frameCount);
        var frameDuration = features.FrameSizeSamples / (double)sampleRate;

        for (var frame = 0; frame < frameCount; frame++)
        {
            var start = frame * features.HopLengthSamples / (double)sampleRate;

            segments.Add(new Segment
            {
                Start = start,
                Duration = frameDuration,
                Confidence = 1.0,
                LoudnessStart = 0.0,
                LoudnessMax = 0.0,
                LoudnessMaxTime = 0.0,
                Timbre = features.Mfcc[frame],
                Pitches = features.Chroma[frame]
            });
        }

        return segments;
    }

    private static IReadOnlyList<Bar> BuildBars(
        IReadOnlyList<Beat> beats,
        int timeSignature)
    {
        if (beats.Count == 0)
        {
            return [];
        }

        var bars = new List<Bar>();
        var beatsPerBar = Math.Max(1, timeSignature);

        for (var i = 0; i < beats.Count; i += beatsPerBar)
        {
            var first = beats[i];
            var lastIndex = Math.Min(i + beatsPerBar - 1, beats.Count - 1);
            var last = beats[lastIndex];
            var end = last.Start + last.Duration;

            bars.Add(new Bar
            {
                Index = bars.Count,
                Start = first.Start,
                Duration = Math.Max(0.0, end - first.Start),
                Confidence = AverageConfidence(beats, i, lastIndex)
            });
        }

        return bars;
    }

    private static IReadOnlyList<Tatum> BuildTatums(IReadOnlyList<Beat> beats)
    {
        return beats
            .Select(beat => new Tatum
            {
                Index = beat.Index,
                Start = beat.Start,
                Duration = beat.Duration,
                Confidence = beat.Confidence
            })
            .ToArray();
    }

    private static IReadOnlyList<Section> BuildSections(LoadedAudio audio, double tempo)
    {
        return
        [
            new Section
            {
                Index = 0,
                Start = 0.0,
                Duration = audio.DurationSeconds,
                Confidence = 1.0,
                Loudness = 0.0,
                Tempo = tempo,
                Label = "Full Track"
            }
        ];
    }

    private static double AverageConfidence(IReadOnlyList<Beat> beats, int firstIndex, int lastIndex)
    {
        var count = lastIndex - firstIndex + 1;
        if (count <= 0)
        {
            return 0.0;
        }

        var sum = 0.0;
        for (var i = firstIndex; i <= lastIndex; i++)
        {
            sum += beats[i].Confidence;
        }

        return sum / count;
    }
}
