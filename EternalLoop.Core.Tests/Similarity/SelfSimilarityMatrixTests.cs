using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class SelfSimilarityMatrixTests
{
    [Fact]
    public void Compute_Should_ReturnEmptyMatrix_WhenNoBeats()
    {
        var matrix = SelfSimilarityMatrix.Compute([], 0.5, 0.5, 0.0, 0.0);

        matrix.GetLength(0).Should().Be(0);
        matrix.GetLength(1).Should().Be(0);
    }

    [Fact]
    public void Compute_Should_SetDiagonalToOne()
    {
        var matrix = SelfSimilarityMatrix.Compute(CreateBeats(), 0.5, 0.5, 0.0, 0.0);

        matrix[0, 0].Should().Be(1.0);
        matrix[1, 1].Should().Be(1.0);
    }

    [Fact]
    public void Compute_Should_BeSymmetric()
    {
        var matrix = SelfSimilarityMatrix.Compute(CreateBeats(), 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().Be(matrix[1, 0]);
    }

    [Fact]
    public void Compute_Should_ReturnHighSimilarity_ForIdenticalFeatures()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 2f], [0f, 1f]),
            CreateBeat(1, [1f, 2f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void Compute_Should_ReturnLowSimilarity_ForOrthogonalFeatures()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [0f, 1f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().Be(0.0);
    }

    [Fact]
    public void Compute_Should_NormalizeWeights()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [1f, 0f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 2.0, 0.0, 0.0, 0.0);

        matrix[0, 1].Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void Compute_Should_FallbackToEqualWeights_WhenWeightsAreZero()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [1f, 0f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.0, 0.0, 0.0, 0.0);

        matrix[0, 1].Should().BeApproximately(1.0 / 3.0, 0.0001);
    }

    [Fact]
    public void Compute_Should_NotThrow_WhenVectorsHaveDifferentLengths()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 2f, 3f], [1f]),
            CreateBeat(1, [1f], [1f, 2f])
        };

        var act = () => SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);

        act.Should().NotThrow();
    }

    [Fact]
    public void Compute_Should_NotReturnNaN()
    {
        var matrix = SelfSimilarityMatrix.Compute(CreateBeats(), 0.5, 0.5, 0.0, 0.0);

        matrix[0, 1].Should().NotBe(double.NaN);
    }

    private static Beat[] CreateBeats()
    {
        return
        [
            CreateBeat(0, [1f, 0f], [1f, 0f]),
            CreateBeat(1, [0f, 1f], [0f, 1f])
        ];
    }

    [Fact]
    public void Compute_Should_AcceptLoudnessWeight_AndAffectSimilarity()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [2f, 2f, 2f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [-2f, -2f, -2f])
        };

        var matrixWithoutLoudness = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);
        var matrixWithLoudness = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.0);

        matrixWithoutLoudness[0, 1].Should().BeApproximately(1.0, 1e-6);
        matrixWithLoudness[0, 1].Should().BeLessThan(matrixWithoutLoudness[0, 1]);
    }

    [Fact]
    public void Compute_Should_HandleAllZeroLoudness_Gracefully()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [0f, 0f, 0f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [0f, 0f, 0f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.0);

        matrix[0, 1].Should().BeInRange(0.0, 1.0);
        matrix[0, 1].Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Compute_Should_NormalizeWeights_WhenSumDiffersFromOne()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [1f, 1f, 1f])
        };

        var matrixA = SelfSimilarityMatrix.Compute(beats, 1.0, 1.0, 1.0, 0.0);
        var matrixB = SelfSimilarityMatrix.Compute(beats, 0.333, 0.333, 0.334, 0.0);

        matrixA[0, 1].Should().BeApproximately(matrixB[0, 1], 1e-3);
    }

    [Fact]
    public void Compute_Should_AcceptBarPositionWeight_AndAffectSimilarity()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [0f, 0f, 0f], [0f, 1f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [0f, 0f, 0f], [0f, -1f])
        };

        var matrixWithoutBarPosition = SelfSimilarityMatrix.Compute(beats, 0.5, 0.5, 0.0, 0.0);
        var matrixWithBarPosition = SelfSimilarityMatrix.Compute(beats, 0.40, 0.30, 0.18, 0.12);

        matrixWithoutBarPosition[0, 1].Should().BeApproximately(1.0, 1e-6);
        matrixWithBarPosition[0, 1].Should().BeLessThan(matrixWithoutBarPosition[0, 1]);
    }

    [Fact]
    public void Compute_Should_NotBoostSimilarity_WhenBarPositionMatches()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(4, [0.7f, 0.3f], [0.7f, 0.3f], [1f, 1f, 1f], [0f, 1f])
        };

        var withoutMetricPenalty = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.0);
        var withMetricPenalty = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.18);

        withMetricPenalty[0, 1].Should().BeApproximately(withoutMetricPenalty[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_Should_PenalizeMetricMismatch_WithoutBoostingMetricMatch()
    {
        var sameMetric = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(4, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f])
        };
        var wrongMetric = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(2, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, -1f])
        };

        var sameMatrix = SelfSimilarityMatrix.Compute(sameMetric, 0.45, 0.35, 0.20, 0.18);
        var wrongMatrix = SelfSimilarityMatrix.Compute(wrongMetric, 0.45, 0.35, 0.20, 0.18);

        sameMatrix[0, 1].Should().BeApproximately(1.0, 1e-6);
        wrongMatrix[0, 1].Should().BeLessThan(sameMatrix[0, 1]);
    }

    [Fact]
    public void Compute_Should_TreatZeroBarPositionVectorAsNeutral()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 0f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 0f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.45, 0.35, 0.20, 0.18);

        matrix[0, 1].Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Compute_Should_NotPenalize_BeatsAtSameMetricPosition()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f]),
            CreateBeat(4, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [0f, 1f])
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, 0.40, 0.30, 0.18, 0.12);

        matrix[0, 1].Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_MatchClassic_WhenAiDisabled()
    {
        var analysis = CreateAnalysisWithAi([1.0f, 0.0f], [0.0f, 1.0f]);
        var options = CreateOptions(
            useAiSimilarity: false,
            useDurationSimilarityGate: false,
            useConfidencePenalty: false);
        var classic = SelfSimilarityMatrix.Compute(analysis.Beats, options.TimbreWeight, options.PitchWeight, options.LoudnessWeight, options.BarPositionWeight);

        var matrix = SelfSimilarityMatrix.Compute(analysis, options);

        matrix[0, 1].Should().BeApproximately(classic[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_MatchClassic_WhenAiDataIsMissing()
    {
        var analysis = CreateAnalysis(ai: null);
        var options = CreateOptions(
            useAiSimilarity: true,
            useDurationSimilarityGate: false,
            useConfidencePenalty: false);
        var classic = SelfSimilarityMatrix.Compute(analysis.Beats, options.TimbreWeight, options.PitchWeight, options.LoudnessWeight, options.BarPositionWeight);

        var matrix = SelfSimilarityMatrix.Compute(analysis, options);

        matrix[0, 1].Should().BeApproximately(classic[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_NotBoost_WhenAiSimilarityIsHigh()
    {
        var analysis = CreateAnalysisWithAi([1.0f, 0.0f], [1.0f, 0.0f]);
        var options = CreateOptions(useAiSimilarity: true);
        var classic = SelfSimilarityMatrix.Compute(analysis.Beats, options.TimbreWeight, options.PitchWeight, options.LoudnessWeight, options.BarPositionWeight);

        var matrix = SelfSimilarityMatrix.Compute(analysis, options);

        matrix[0, 1].Should().BeApproximately(classic[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_Penalize_WhenAiSimilarityIsMedium()
    {
        var analysis = CreateAnalysisWithAi([1.0f, 0.0f], [0.65f, 0.760f]);
        var options = CreateOptions(useAiSimilarity: true);
        var classic = SelfSimilarityMatrix.Compute(analysis.Beats, options.TimbreWeight, options.PitchWeight, options.LoudnessWeight, options.BarPositionWeight);

        var matrix = SelfSimilarityMatrix.Compute(analysis, options);

        matrix[0, 1].Should().BeLessThan(classic[0, 1]);
        matrix[0, 1].Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_Reject_WhenAiSimilarityIsLow()
    {
        var analysis = CreateAnalysisWithAi([1.0f, 0.0f], [0.0f, 1.0f]);
        var matrix = SelfSimilarityMatrix.Compute(analysis, CreateOptions(useAiSimilarity: true));

        matrix[0, 1].Should().Be(0.0);
        matrix[1, 0].Should().Be(0.0);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_UseBeatIndexMappingForAiEmbeddings()
    {
        var beats = new[]
        {
            CreateBeat(10, [1f, 0f], [1f, 0f]),
            CreateBeat(20, [1f, 0f], [1f, 0f])
        };
        var analysis = CreateAnalysis(new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings =
            [
                new AiBeatEmbedding { BeatIndex = 20, Vector = [0.0f, 1.0f] },
                new AiBeatEmbedding { BeatIndex = 10, Vector = [1.0f, 0.0f] }
            ]
        }, beats);

        var matrix = SelfSimilarityMatrix.Compute(analysis, CreateOptions(useAiSimilarity: true));

        matrix[0, 1].Should().Be(0.0);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_HandleMissingBeatEmbeddingsAsClassic()
    {
        var analysis = CreateAnalysis(new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings =
            [
                new AiBeatEmbedding { BeatIndex = 0, Vector = [1.0f, 0.0f] }
            ]
        });
        var options = CreateOptions(useAiSimilarity: true);
        var classic = SelfSimilarityMatrix.Compute(analysis.Beats, options.TimbreWeight, options.PitchWeight, options.LoudnessWeight, options.BarPositionWeight);

        var matrix = SelfSimilarityMatrix.Compute(analysis, options);

        matrix[0, 1].Should().BeApproximately(classic[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_Should_NormalizeFourWeights_WhenSumDiffersFromOne()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [1f, 0f]),
            CreateBeat(1, [1f, 0f], [1f, 0f], [1f, 1f, 1f], [1f, 0f])
        };

        var matrixA = SelfSimilarityMatrix.Compute(beats, 1.0, 1.0, 1.0, 1.0);
        var matrixB = SelfSimilarityMatrix.Compute(beats, 0.25, 0.25, 0.25, 0.25);

        matrixA[0, 1].Should().BeApproximately(matrixB[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_WithOptions_Should_MatchLegacy_WhenDurationAndConfidenceFiltersDisabled()
    {
        var beats = new[] { CreateBeat(0, [1f, 0f], [1f, 0f]), CreateBeat(1, [1f, 0f], [1f, 0f]) };
        var options = CreateOptions(
            useAiSimilarity: false,
            useDurationSimilarityGate: false,
            useConfidencePenalty: false);
        var legacy = SelfSimilarityMatrix.Compute(
            beats,
            options.TimbreWeight,
            options.PitchWeight,
            options.LoudnessWeight,
            options.BarPositionWeight);

        var optionsAware = SelfSimilarityMatrix.Compute(beats, options);

        optionsAware[0, 1].Should().BeApproximately(legacy[0, 1], 1e-6);
    }

    [Fact]
    public void Compute_WithOptions_Should_PenalizeDurationMismatch()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], duration: 0.50),
            CreateBeat(1, [1f, 0f], [1f, 0f], duration: 0.43)
        };
        var options = CreateOptions(useAiSimilarity: false);
        var legacy = SelfSimilarityMatrix.Compute(beats, 1.0, 0.0, 0.0, 0.0);

        var matrix = SelfSimilarityMatrix.Compute(beats, options);

        matrix[0, 1].Should().BeLessThan(legacy[0, 1]);
        matrix[0, 1].Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void Compute_WithOptions_Should_RejectDurationMismatchBelowThreshold()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], duration: 0.50),
            CreateBeat(1, [1f, 0f], [1f, 0f], duration: 0.35)
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, CreateOptions(useAiSimilarity: false));

        matrix[0, 1].Should().Be(0.0);
        matrix[1, 0].Should().Be(0.0);
    }

    [Fact]
    public void Compute_WithOptions_Should_PenalizeLowConfidencePair()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], confidence: 0.40),
            CreateBeat(1, [1f, 0f], [1f, 0f], confidence: 0.80)
        };
        var options = CreateOptions(useAiSimilarity: false);
        var legacy = SelfSimilarityMatrix.Compute(beats, 1.0, 0.0, 0.0, 0.0);

        var matrix = SelfSimilarityMatrix.Compute(beats, options);

        matrix[0, 1].Should().BeLessThan(legacy[0, 1]);
        matrix[0, 1].Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void Compute_WithOptions_Should_NeverBoostAboveLegacyScore()
    {
        var beats = new[] { CreateBeat(0, [1f, 0f], [1f, 0f]), CreateBeat(1, [1f, 0f], [1f, 0f]) };
        var options = CreateOptions(useAiSimilarity: false);
        var legacy = SelfSimilarityMatrix.Compute(beats, 1.0, 0.0, 0.0, 0.0);

        var matrix = SelfSimilarityMatrix.Compute(beats, options);

        matrix[0, 1].Should().BeLessThanOrEqualTo(legacy[0, 1]);
    }

    [Fact]
    public void Compute_WithOptions_Should_NotReturnNaN_WhenDurationOrConfidenceInvalid()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], duration: double.NaN, confidence: double.NaN),
            CreateBeat(1, [1f, 0f], [1f, 0f], duration: 0.50, confidence: 1.0)
        };

        var matrix = SelfSimilarityMatrix.Compute(beats, CreateOptions(useAiSimilarity: false));

        matrix[0, 1].Should().NotBe(double.NaN);
        matrix[0, 1].Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Compute_WithTrackAnalysis_Should_ComposeAiDurationAndConfidenceWithoutBoosting()
    {
        var beats = new[]
        {
            CreateBeat(0, [1f, 0f], [1f, 0f], duration: 0.50, confidence: 0.40),
            CreateBeat(1, [1f, 0f], [1f, 0f], duration: 0.43, confidence: 0.80)
        };
        var analysis = CreateAnalysis(new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings =
            [
                new AiBeatEmbedding { BeatIndex = 0, Vector = [1.0f, 0.0f] },
                new AiBeatEmbedding { BeatIndex = 1, Vector = [1.0f, 0.0f] }
            ]
        }, beats);
        var options = CreateOptions(useAiSimilarity: true);
        var withoutBranchQuality = SelfSimilarityMatrix.Compute(analysis, CreateOptions(
            useAiSimilarity: true,
            useDurationSimilarityGate: false,
            useConfidencePenalty: false));

        var matrix = SelfSimilarityMatrix.Compute(analysis, options);

        matrix[0, 1].Should().BeLessThan(withoutBranchQuality[0, 1]);
        matrix[0, 1].Should().BeLessThanOrEqualTo(withoutBranchQuality[0, 1]);
        matrix[0, 1].Should().BeInRange(0.0, 1.0);
    }

    private static Beat CreateBeat(
        int index,
        float[] timbre,
        float[] pitches,
        float[]? loudness = null,
        float[]? barPosition = null,
        double duration = 0.5,
        double confidence = 1.0)
    {
        return new Beat
        {
            Index = index,
            Start = index * 0.5,
            Duration = duration,
            Confidence = confidence,
            Timbre = timbre,
            Pitches = pitches,
            Loudness = loudness ?? [0f, 0f, 0f],
            BarPosition = barPosition ?? [0f, 0f]
        };
    }

    private static BranchFindingOptions CreateOptions(
        bool useAiSimilarity,
        bool useDurationSimilarityGate = true,
        bool useConfidencePenalty = true)
    {
        return new BranchFindingOptions
        {
            UseAiSimilarity = useAiSimilarity,
            UseDurationSimilarityGate = useDurationSimilarityGate,
            UseConfidencePenalty = useConfidencePenalty,
            TimbreWeight = 1.0,
            PitchWeight = 0.0,
            LoudnessWeight = 0.0,
            BarPositionWeight = 0.0,
            AiRejectionThreshold = 0.58,
            AiPenaltyStartThreshold = 0.72,
            AiPenaltyStrength = 0.22
        };
    }

    private static TrackAnalysis CreateAnalysisWithAi(float[] firstEmbedding, float[] secondEmbedding)
    {
        return CreateAnalysis(new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings =
            [
                new AiBeatEmbedding { BeatIndex = 0, Vector = firstEmbedding },
                new AiBeatEmbedding { BeatIndex = 1, Vector = secondEmbedding }
            ]
        });
    }

    private static TrackAnalysis CreateAnalysis(AiAnalysisData? ai, IReadOnlyList<Beat>? beats = null)
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "track.wav",
                DurationSeconds = 1.0,
                SampleRate = 22_050,
                Tempo = 120.0,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = beats ?? [CreateBeat(0, [1f, 0f], [1f, 0f]), CreateBeat(1, [1f, 0f], [1f, 0f])],
            Bars = [],
            Tatums = [],
            Sections = [],
            Ai = ai
        };
    }
}
