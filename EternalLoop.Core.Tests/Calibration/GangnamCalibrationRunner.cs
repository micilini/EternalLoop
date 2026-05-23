using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Analysis;
using EternalLoop.Core.Audio;
using EternalLoop.Core.BeatTracking;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Similarity;
using Microsoft.Extensions.Options;

namespace EternalLoop.Core.Tests.Calibration;

public sealed class GangnamCalibrationRunner
{
    private const int AnalysisSampleRate = 22_050;
    private const int DefaultTimeSignature = 4;

    public async Task<GangnamCalibrationRunResult> AnalyzeAsync(
        string audioPath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var loader = new NAudioAudioLoader(Options.Create(new AudioLoaderOptions()));
        var extractor = new NWavesFeatureExtractor();
        var beatTracker = new SpectralFluxBeatTracker();
        var audio = await loader.LoadAsync(audioPath, AnalysisSampleRate, cancellationToken).ConfigureAwait(false);
        var features = extractor.Extract(audio, new FeatureExtractionOptions());
        var beatTracking = beatTracker.Track(audio, features, new BeatTrackingOptions());
        var beats = BeatFeatureAggregator.AggregateFeatures(
            beatTracking,
            features,
            audio.SampleRate,
            timeSignature: DefaultTimeSignature);
        var microFingerprints = BeatMicrosegmentExtractor.Extract(
            beats,
            features,
            audio.SampleRate,
            TuningDefaultValues.MicrosegmentCount);

        var analysis = new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = audio.FileHash,
                FilePath = Path.GetFileName(audioPath),
                DurationSeconds = audio.DurationSeconds,
                SampleRate = audio.SampleRate,
                Tempo = beatTracking.EstimatedBpm,
                TimeSignature = DefaultTimeSignature,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion,
                AnalyzedAt = DateTime.UtcNow
            },
            Segments = [],
            Beats = beats,
            Bars = [],
            Tatums = [],
            Sections = [],
            MicroFingerprints = microFingerprints
        };

        return new GangnamCalibrationRunResult(
            analysis,
            RunPreset(analysis, TuningPresetCatalog.BalancedId, outputDirectory),
            RunPreset(analysis, TuningPresetCatalog.WildId, outputDirectory));
    }

    public EternalLoopBranchSummary RunPreset(
        TrackAnalysis analysis,
        string presetId,
        string outputDirectory,
        BranchFindingOptions? overrideOptions = null,
        bool writeDiagnostics = true)
    {
        var options = overrideOptions ?? CreatePresetOptions(presetId);
        var finder = new CosineSimilarityBranchFinder();
        var edges = finder.FindBranches(analysis, options);
        var graph = BuildGraph(analysis.Beats, edges, options);
        var previous = Environment.GetEnvironmentVariable("ETERNALLOOP_EXPORT_BRANCH_CSV");

        try
        {
            BranchQualityDiagnosticResult? diagnostic = null;
            if (writeDiagnostics)
            {
                Environment.SetEnvironmentVariable("ETERNALLOOP_EXPORT_BRANCH_CSV", "1");
                diagnostic = BranchQualityDiagnosticWriter.WriteIfEnabled(
                    analysis,
                    graph,
                    options,
                    Path.Combine(outputDirectory, presetId.ToLowerInvariant()));
            }

            return CreateSummary(
                presetId,
                options.UseAiSimilarity,
                analysis,
                edges,
                diagnostic?.CsvPath ?? string.Empty,
                diagnostic?.SummaryPath ?? string.Empty);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ETERNALLOOP_EXPORT_BRANCH_CSV", previous);
        }
    }

    public static BranchFindingOptions CreatePresetOptions(string presetId)
    {
        var preset = TuningPresetCatalog.GetById(presetId);
        var settings = new UserSettings
        {
            Preset = preset.Id,
            UseAiSimilarity = false
        };
        TuningOptionsMapper.ApplyPreset(settings, preset);
        var options = TuningOptionsMapper.ToBranchFindingOptions(settings);

        return new BranchFindingOptions
        {
            SimilarityThreshold = options.SimilarityThreshold,
            LookaheadDepth = options.LookaheadDepth,
            MinJumpDistance = options.MinJumpDistance,
            MaxBranchesPerBeat = options.MaxBranchesPerBeat,
            LandingOffsetBeats = options.LandingOffsetBeats,
            ContinuationLookaheadDepth = options.ContinuationLookaheadDepth,
            ContinuationThresholdMargin = options.ContinuationThresholdMargin,
            AnchorLookaheadPassRatio = options.AnchorLookaheadPassRatio,
            AnchorLookaheadDropTolerance = options.AnchorLookaheadDropTolerance,
            ContinuationLookaheadPassRatio = options.ContinuationLookaheadPassRatio,
            ContinuationLookaheadDropTolerance = options.ContinuationLookaheadDropTolerance,
            TimbreWeight = options.TimbreWeight,
            PitchWeight = options.PitchWeight,
            LoudnessWeight = options.LoudnessWeight,
            BarPositionWeight = options.BarPositionWeight,
            UseAiSimilarity = false,
            AiRejectionThreshold = options.AiRejectionThreshold,
            AiPenaltyStartThreshold = options.AiPenaltyStartThreshold,
            AiPenaltyStrength = options.AiPenaltyStrength,
            UseDurationSimilarityGate = options.UseDurationSimilarityGate,
            DurationPenaltyStartRatio = options.DurationPenaltyStartRatio,
            DurationRejectionRatio = options.DurationRejectionRatio,
            DurationPenaltyStrength = options.DurationPenaltyStrength,
            UseConfidencePenalty = options.UseConfidencePenalty,
            ConfidencePenaltyStart = options.ConfidencePenaltyStart,
            ConfidenceRejectionThreshold = options.ConfidenceRejectionThreshold,
            ConfidencePenaltyStrength = options.ConfidencePenaltyStrength,
            MetricPositionMode = options.MetricPositionMode,
            MetricPositionPenaltyStrength = options.MetricPositionPenaltyStrength,
            MetricPositionRejectionThreshold = options.MetricPositionRejectionThreshold,
            TargetBranchSourceRatio = options.TargetBranchSourceRatio,
            MaxBranchSourceRatio = options.MaxBranchSourceRatio,
            UseMicrosegmentSimilarity = options.UseMicrosegmentSimilarity,
            MicrosegmentCount = options.MicrosegmentCount,
            MicrosegmentPenaltyStartThreshold = options.MicrosegmentPenaltyStartThreshold,
            MicrosegmentRejectionThreshold = options.MicrosegmentRejectionThreshold,
            MicrosegmentPenaltyStrength = options.MicrosegmentPenaltyStrength
        };
    }

    private static JukeboxGraph BuildGraph(
        IReadOnlyList<Beat> beats,
        IReadOnlyList<JukeboxEdge> edges,
        BranchFindingOptions options)
    {
        var nodes = beats
            .OrderBy(beat => beat.Index)
            .Select(beat => new JukeboxNode
            {
                BeatIndex = beat.Index,
                Start = beat.Start,
                Duration = beat.Duration
            })
            .ToArray();
        var validBeatIndexes = nodes.Select(node => node.BeatIndex).ToHashSet();
        var jumpEdges = edges
            .Where(edge =>
                validBeatIndexes.Contains(edge.FromBeat) &&
                validBeatIndexes.Contains(edge.ToBeat) &&
                edge.FromBeat != edge.ToBeat)
            .GroupBy(edge => edge.FromBeat)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(edge => edge.Similarity)
                    .ThenBy(edge => edge.ToBeat)
                    .ToList());

        return new JukeboxGraph
        {
            Nodes = nodes,
            JumpEdges = jumpEdges,
            SimilarityThreshold = options.SimilarityThreshold,
            LookaheadDepth = options.LookaheadDepth
        };
    }

    private static EternalLoopBranchSummary CreateSummary(
        string preset,
        bool useAi,
        TrackAnalysis analysis,
        IReadOnlyList<JukeboxEdge> edges,
        string csvPath,
        string summaryPath)
    {
        var sourceCount = edges.Select(edge => edge.FromBeat).Distinct().Count();
        var timeSignature = Math.Max(1, analysis.Metadata.TimeSignature);
        var longBackwardDistance = analysis.Beats.Count * 0.10;

        return new EternalLoopBranchSummary(
            preset,
            useAi,
            analysis.Metadata.DurationSeconds,
            analysis.Metadata.Tempo,
            analysis.Beats.Count,
            edges.Count,
            sourceCount,
            analysis.Beats.Count == 0 ? 0.0 : sourceCount / (double)analysis.Beats.Count,
            edges.Count(edge => edge.ToBeat < edge.FromBeat),
            edges.Count(edge => edge.ToBeat > edge.FromBeat),
            edges.Count(edge => edge.ToBeat < edge.FromBeat && Math.Abs(edge.ToBeat - edge.FromBeat) >= longBackwardDistance),
            edges.Count(edge => Math.Abs(edge.FromBeat % timeSignature) == Math.Abs(edge.ToBeat % timeSignature)),
            csvPath,
            summaryPath);
    }
}

public sealed record GangnamCalibrationRunResult(
    TrackAnalysis Analysis,
    EternalLoopBranchSummary Balanced,
    EternalLoopBranchSummary Wild);
