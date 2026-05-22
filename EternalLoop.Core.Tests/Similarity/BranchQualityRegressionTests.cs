using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class BranchQualityRegressionTests
{
    private const int SampleRate = 22_050;
    private const int HopLengthSamples = 2_756;
    private const int FramesPerBeat = 4;
    private const int PhraseLengthBeats = 16;
    private const double ScoreEpsilon = 0.000001;

    [Fact]
    public void Presets_Should_Produce_Monotonic_EdgeCounts_From_Conservative_To_Wild()
    {
        var analysis = CreateAnalysis(beatCount: 96, variant: TrackVariant.Healthy, includeAi: false);
        var finder = new CosineSimilarityBranchFinder();

        var conservative = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.ConservativeId, useAiSimilarity: false));
        var balanced = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: false));
        var wild = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.WildId, useAiSimilarity: false));

        conservative.Count.Should().BeLessThanOrEqualTo(balanced.Count);
        balanced.Count.Should().BeLessThanOrEqualTo(wild.Count);
        balanced.Should().NotBeEmpty();
        wild.Should().NotBeEmpty();
    }

    [Fact]
    public void Presets_Should_Produce_Monotonic_SourceDensity_From_Conservative_To_Wild()
    {
        var analysis = CreateAnalysis(beatCount: 96, variant: TrackVariant.Healthy, includeAi: false);
        var finder = new CosineSimilarityBranchFinder();
        var conservativeOptions = CreatePresetOptions(TuningPresetCatalog.ConservativeId, useAiSimilarity: false);
        var balancedOptions = CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: false);
        var wildOptions = CreatePresetOptions(TuningPresetCatalog.WildId, useAiSimilarity: false);

        var conservative = finder.FindBranches(analysis, conservativeOptions);
        var balanced = finder.FindBranches(analysis, balancedOptions);
        var wild = finder.FindBranches(analysis, wildOptions);

        CountSources(conservative).Should().BeLessThanOrEqualTo(CountSources(balanced));
        CountSources(balanced).Should().BeLessThanOrEqualTo(CountSources(wild));
        conservative.GroupBy(edge => edge.FromBeat).Should().OnlyContain(group => group.Count() <= conservativeOptions.MaxBranchesPerBeat);
        balanced.GroupBy(edge => edge.FromBeat).Should().OnlyContain(group => group.Count() <= balancedOptions.MaxBranchesPerBeat);
        wild.GroupBy(edge => edge.FromBeat).Should().OnlyContain(group => group.Count() <= wildOptions.MaxBranchesPerBeat);
    }

    [Fact]
    public void Balanced_Should_Not_Zero_Edges_For_Healthy_Repeated_Track()
    {
        var analysis = CreateAnalysis(beatCount: 64, variant: TrackVariant.Healthy, includeAi: false);
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: false));

        edges.Should().NotBeEmpty();
        CountSources(edges).Should().BeGreaterThan(0);
        edges.Should().OnlyContain(edge => HasValidScore(edge));
    }

    [Fact]
    public void Wild_Should_Keep_Edges_When_Balanced_Is_Strict()
    {
        var analysis = CreateAnalysis(beatCount: 96, variant: TrackVariant.Difficult, includeAi: false);
        var finder = new CosineSimilarityBranchFinder();

        var balanced = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: false));
        var wild = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.WildId, useAiSimilarity: false));

        wild.Count.Should().BeGreaterThanOrEqualTo(balanced.Count);
        wild.Should().NotBeEmpty();
    }

    [Fact]
    public void BranchQualityFilters_Should_Never_Increase_MatrixScores()
    {
        var analysis = CreateAnalysis(beatCount: 32, variant: TrackVariant.Difficult, includeAi: false);
        var baselineOptions = CreateBaselineOptions();
        var filteredOptions = new BranchFindingOptions
        {
            UseAiSimilarity = false,
            UseDurationSimilarityGate = true,
            UseConfidencePenalty = true,
            MetricPositionMode = MetricPositionMode.StrongPenalty,
            MetricPositionPenaltyStrength = 0.32,
            MetricPositionRejectionThreshold = 0.20,
            UseMicrosegmentSimilarity = true,
            MicrosegmentCount = 4,
            MicrosegmentPenaltyStartThreshold = 0.80,
            MicrosegmentRejectionThreshold = 0.64,
            MicrosegmentPenaltyStrength = 0.18,
            TimbreWeight = 1.0,
            PitchWeight = 0.0,
            LoudnessWeight = 0.0,
            BarPositionWeight = 0.0
        };

        var baseline = SelfSimilarityMatrix.Compute(analysis, baselineOptions);
        var filtered = SelfSimilarityMatrix.Compute(analysis, filteredOptions);

        for (var row = 0; row < baseline.GetLength(0); row++)
        {
            for (var column = 0; column < baseline.GetLength(1); column++)
            {
                if (row == column)
                {
                    continue;
                }

                filtered[row, column].Should().BeLessThanOrEqualTo(baseline[row, column] + ScoreEpsilon);
            }
        }
    }

    [Fact]
    public void BranchFinder_Should_Preserve_Classic_Behavior_When_NewFiltersAreDisabled()
    {
        var analysis = CreateAnalysis(beatCount: 64, variant: TrackVariant.Healthy, includeAi: false);
        var finder = new CosineSimilarityBranchFinder();
        var options = CreateBaselineOptions();

        var beatEdges = finder.FindBranches(analysis.Beats, options);
        var analysisEdges = finder.FindBranches(analysis, options);

        analysisEdges.Should().NotBeEmpty();
        analysisEdges.Select(edge => (edge.FromBeat, edge.ToBeat))
            .Should().Equal(beatEdges.Select(edge => (edge.FromBeat, edge.ToBeat)));
        analysisEdges.Should().OnlyContain(edge => HasValidScore(edge));
    }

    [Fact]
    public void DurationConfidenceMetricMicrosegmentFilters_Should_Compose_Safely()
    {
        var analysis = CreateAnalysis(beatCount: 32, variant: TrackVariant.Difficult, includeAi: false);
        var options = CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: false);

        var act = () => SelfSimilarityMatrix.Compute(analysis, options);

        var matrix = act.Should().NotThrow().Subject;
        matrix.GetLength(0).Should().Be(analysis.Beats.Count);
        matrix.GetLength(1).Should().Be(analysis.Beats.Count);

        for (var row = 0; row < matrix.GetLength(0); row++)
        {
            matrix[row, row].Should().Be(1.0);
            for (var column = 0; column < matrix.GetLength(1); column++)
            {
                matrix[row, column].Should().Be(matrix[column, row]);
                matrix[row, column].Should().BeInRange(0.0, 1.0);
                double.IsFinite(matrix[row, column]).Should().BeTrue();
            }
        }
    }

    [Fact]
    public void AiSimilarity_Should_Compose_With_BranchQualityFilters()
    {
        var analysis = CreateAnalysis(beatCount: 64, variant: TrackVariant.Healthy, includeAi: true);
        var finder = new CosineSimilarityBranchFinder();
        var options = CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: true);

        var act = () => finder.FindBranches(analysis, options);

        var edges = act.Should().NotThrow().Subject;
        edges.Should().OnlyContain(edge => HasValidScore(edge));
    }

    [Fact]
    public void AiOff_Should_Still_Use_BranchQualityFilters()
    {
        var analysis = CreateAnalysis(beatCount: 64, variant: TrackVariant.Difficult, includeAi: true);
        var finder = new CosineSimilarityBranchFinder();
        var filteredOptions = CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: false);
        var baselineOptions = CreateBaselineOptions();

        var filtered = finder.FindBranches(analysis, filteredOptions);
        var baseline = finder.FindBranches(analysis.Beats, baselineOptions);

        filtered.Count.Should().BeLessThanOrEqualTo(baseline.Count);
        filtered.Should().OnlyContain(edge => HasValidScore(edge));
    }

    [Fact]
    public void Wild_should_not_produce_fewer_edges_than_balanced_on_dense_repeated_material()
    {
        var analysis = CreateAnalysis(beatCount: 192, variant: TrackVariant.Healthy, includeAi: false);
        var finder = new CosineSimilarityBranchFinder();

        var balanced = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.BalancedId, useAiSimilarity: false));
        var wild = finder.FindBranches(analysis, CreatePresetOptions(TuningPresetCatalog.WildId, useAiSimilarity: false));

        wild.Count.Should().BeGreaterThanOrEqualTo(balanced.Count);
        CountSources(wild).Should().BeGreaterThanOrEqualTo(CountSources(balanced));
        wild.Should().NotBeEmpty();
    }

    private static TrackAnalysis CreateAnalysis(int beatCount, TrackVariant variant, bool includeAi)
    {
        var beatTracking = CreateBeatTracking(beatCount, variant);
        var features = CreateFeatureMatrix(beatCount, variant);
        var beats = BeatFeatureAggregator.AggregateFeatures(beatTracking, features, SampleRate);
        var microFingerprints = BeatMicrosegmentExtractor.Extract(beats, features, SampleRate, TuningDefaultValues.MicrosegmentCount);

        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "synthetic",
                FilePath = "synthetic.wav",
                DurationSeconds = beatCount * 0.5,
                SampleRate = SampleRate,
                Tempo = 120.0,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = beats,
            Bars = [],
            Tatums = [],
            Sections = [],
            MicroFingerprints = microFingerprints,
            Ai = includeAi ? CreateAiData(beats, variant) : null
        };
    }

    private static BeatTrackingResult CreateBeatTracking(int beatCount, TrackVariant variant)
    {
        var times = new double[beatCount];
        var confidences = new double[beatCount];
        var current = 0.0;

        for (var i = 0; i < beatCount; i++)
        {
            times[i] = current;
            var variation = variant == TrackVariant.Difficult && i % PhraseLengthBeats is 5 or 11 ? 0.015 : 0.0;
            current += 0.5 + variation;
            confidences[i] = variant == TrackVariant.Difficult && i % PhraseLengthBeats is 7 or 13 ? 0.68 : 1.0;
        }

        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = times,
            Confidences = confidences
        };
    }

    private static FeatureMatrix CreateFeatureMatrix(int beatCount, TrackVariant variant)
    {
        var frameCount = beatCount * FramesPerBeat;
        var mfcc = new float[frameCount][];
        var chroma = new float[frameCount][];
        var rms = new float[frameCount];
        var flux = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            var beatIndex = frame / FramesPerBeat;
            var microIndex = frame % FramesPerBeat;
            var phraseBeat = beatIndex % PhraseLengthBeats;
            var phraseGroup = phraseBeat % 8;
            var variantOffset = variant == TrackVariant.Difficult && beatIndex >= beatCount / 2 && microIndex == 2
                ? 0.18f
                : 0f;

            mfcc[frame] = CreateVector(phraseGroup, microIndex, variantOffset);
            chroma[frame] = CreateVector((phraseGroup + 3) % 8, microIndex, variantOffset * 0.5f);
            rms[frame] = 0.7f + (phraseGroup * 0.02f) + (microIndex * 0.01f);
            flux[frame] = 0.2f + (microIndex * 0.05f) + variantOffset;
        }

        return new FeatureMatrix
        {
            Mfcc = mfcc,
            Chroma = chroma,
            SpectralFlux = flux,
            Rms = rms,
            HopLengthSamples = HopLengthSamples,
            FrameSizeSamples = 2_048
        };
    }

    private static float[] CreateVector(int primary, int microIndex, float offset)
    {
        var vector = new float[8];
        vector[primary] = 1.0f;
        vector[(primary + microIndex + 1) % vector.Length] = 0.35f + offset;
        vector[(primary + 3) % vector.Length] = 0.12f;
        return vector;
    }

    private static BranchFindingOptions CreatePresetOptions(string presetId, bool useAiSimilarity)
    {
        var preset = TuningPresetCatalog.GetById(presetId);
        var settings = new UserSettings
        {
            Preset = preset.Id,
            UseAiSimilarity = useAiSimilarity
        };
        TuningOptionsMapper.ApplyPreset(settings, preset);
        var options = TuningOptionsMapper.ToBranchFindingOptions(settings);

        return new BranchFindingOptions
        {
            SimilarityThreshold = options.SimilarityThreshold,
            LookaheadDepth = options.LookaheadDepth,
            MinJumpDistance = options.MinJumpDistance,
            TimbreWeight = options.TimbreWeight,
            PitchWeight = options.PitchWeight,
            LoudnessWeight = options.LoudnessWeight,
            BarPositionWeight = options.BarPositionWeight,
            MaxBranchesPerBeat = options.MaxBranchesPerBeat,
            LandingOffsetBeats = options.LandingOffsetBeats,
            ContinuationLookaheadDepth = options.ContinuationLookaheadDepth,
            ContinuationThresholdMargin = options.ContinuationThresholdMargin,
            UseAiSimilarity = options.UseAiSimilarity,
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

    private static BranchFindingOptions CreateBaselineOptions()
    {
        return new BranchFindingOptions
        {
            UseAiSimilarity = false,
            UseDurationSimilarityGate = false,
            UseConfidencePenalty = false,
            MetricPositionMode = MetricPositionMode.Disabled,
            UseMicrosegmentSimilarity = false,
            SimilarityThreshold = 0.0,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 1,
            MaxBranchesPerBeat = 5,
            LandingOffsetBeats = 0,
            TargetBranchSourceRatio = 1.0,
            MaxBranchSourceRatio = 1.0,
            TimbreWeight = 1.0,
            PitchWeight = 0.0,
            LoudnessWeight = 0.0,
            BarPositionWeight = 0.0
        };
    }

    private static AiAnalysisData CreateAiData(IReadOnlyList<Beat> beats, TrackVariant variant)
    {
        return new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings = beats
                .Select(beat =>
                {
                    var vector = new float[8];
                    var phraseBeat = beat.Index % PhraseLengthBeats;
                    vector[phraseBeat % vector.Length] = 1.0f;
                    if (variant == TrackVariant.Difficult && beat.Index % PhraseLengthBeats == 11)
                    {
                        vector[(phraseBeat + 2) % vector.Length] = 0.25f;
                    }

                    return new AiBeatEmbedding
                    {
                        BeatIndex = beat.Index,
                        Vector = vector
                    };
                })
                .ToArray()
        };
    }

    private static int CountSources(IReadOnlyList<JukeboxEdge> edges)
    {
        return edges.Select(edge => edge.FromBeat).Distinct().Count();
    }

    private static bool HasValidScore(JukeboxEdge edge)
    {
        return double.IsFinite(edge.Similarity) &&
            edge.Similarity >= 0.0 &&
            edge.Similarity <= 1.0;
    }

    private enum TrackVariant
    {
        Healthy,
        Difficult
    }
}
