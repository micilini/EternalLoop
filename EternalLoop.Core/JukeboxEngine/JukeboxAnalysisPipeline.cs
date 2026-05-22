using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.BeatTracking;
using EternalLoop.Core.Similarity;
using Microsoft.Extensions.Logging;

namespace EternalLoop.Core.JukeboxEngine;

public sealed class JukeboxAnalysisPipeline : IJukeboxAnalysisPipeline
{
    private const int AnalysisSampleRate = 22_050;
    private const int DefaultTimeSignature = 4;

    private readonly IAudioLoader _audioLoader;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IBeatTracker _beatTracker;
    private readonly IBranchFinder _branchFinder;
    private readonly ITrackAnalysisCache _cache;
    private readonly ILogger<JukeboxAnalysisPipeline> _logger;

    public JukeboxAnalysisPipeline(
        IAudioLoader audioLoader,
        IFeatureExtractor featureExtractor,
        IBeatTracker beatTracker,
        IBranchFinder branchFinder,
        ITrackAnalysisCache cache,
        ILogger<JukeboxAnalysisPipeline> logger)
    {
        _audioLoader = audioLoader ?? throw new ArgumentNullException(nameof(audioLoader));
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _beatTracker = beatTracker ?? throw new ArgumentNullException(nameof(beatTracker));
        _branchFinder = branchFinder ?? throw new ArgumentNullException(nameof(branchFinder));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
                else
                {
                    _logger.LogInformation("Track analysis cache hit for {FileHash}", audio.FileHash);

                    progressReporter.Report(AnalysisStage.BuildingGraph, 0.0, "Loading loop map");
                    var cachedGraph = BuildGraph(cachedAnalysis.Beats, effectiveBranchOptions);
                    progressReporter.Report(AnalysisStage.BuildingGraph, 1.0, "Loop map ready");
                    progressReporter.Report(AnalysisStage.Done, 1.0, "Loaded from cache");

                    return new JukeboxAnalysisResult
                    {
                        Audio = audio,
                        Analysis = cachedAnalysis,
                        Graph = cachedGraph,
                        LoadedFromCache = true
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

        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.BuildingGraph, 0.0, "Building graph");
        var analysis = BuildAnalysis(filePath, audio, beatTracking, features, beats);
        await _cache.SaveAsync(analysis, cancellationToken).ConfigureAwait(false);
        var graph = BuildGraph(beats, effectiveBranchOptions);
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
            LoadedFromCache = false
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

    private static TrackAnalysis BuildAnalysis(
        string filePath,
        LoadedAudio audio,
        BeatTrackingResult beatTracking,
        FeatureMatrix features,
        IReadOnlyList<Beat> beats)
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
            Sections = BuildSections(audio, beatTracking.EstimatedBpm)
        };
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
