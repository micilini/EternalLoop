using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class BranchFinderPipelineSmokeTests
{
    [Fact]
    public void F5ToF6Pipeline_Should_AggregateBeatsAndFindBranches()
    {
        var beatTracking = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 16).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 16).ToArray()
        };
        var features = CreateRepeatedFeatureMatrix();

        var beats = BeatFeatureAggregator.AggregateFeatures(beatTracking, features, 22_050);
        var finder = new CosineSimilarityBranchFinder();
        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.9,
            LookaheadDepth = 2,
            MinJumpDistance = 6
        });

        edges.Should().NotBeEmpty();
        edges.Should().OnlyContain(edge =>
            edge.FromBeat >= 0 &&
            edge.ToBeat >= 0 &&
            edge.FromBeat < beats.Count &&
            edge.ToBeat < beats.Count &&
            edge.Similarity >= 0.0 &&
            edge.Similarity <= 1.0);
    }

    [Fact]
    public void F7Pipeline_Should_AggregateBeatsUseAiEmbeddingsAndFindBranches()
    {
        var beatTracking = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 16).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 16).ToArray()
        };
        var features = CreateRepeatedFeatureMatrix();
        var beats = BeatFeatureAggregator.AggregateFeatures(beatTracking, features, 22_050);
        var analysis = CreateAnalysis(beats);
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(analysis, new BranchFindingOptions
        {
            UseAiSimilarity = true,
            SimilarityThreshold = 0.9,
            LookaheadDepth = 2,
            MinJumpDistance = 6,
            LandingOffsetBeats = 0
        });

        edges.Should().NotBeEmpty();
        edges.Should().OnlyContain(edge =>
            edge.Similarity >= 0.0 &&
            edge.Similarity <= 1.0);
        edges.Should().NotContain(edge => edge.FromBeat == 0 && edge.ToBeat == 8);
    }

    [Fact]
    public void F2Pipeline_Should_PenalizeDurationAndConfidenceWithoutCrashing()
    {
        var beatTracking = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 16).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 16).ToArray()
        };
        var features = CreateRepeatedFeatureMatrix();
        var beats = BeatFeatureAggregator.AggregateFeatures(beatTracking, features, 22_050)
            .Select(beat => beat.Index >= 8 && beat.Index <= 10
                ? CopyBeat(beat, duration: 0.25, confidence: 0.10)
                : beat)
            .ToArray();
        var finder = new CosineSimilarityBranchFinder();

        IReadOnlyList<JukeboxEdge>? edges = null;
        var act = () => edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.80,
            LookaheadDepth = 2,
            MinJumpDistance = 6,
            LandingOffsetBeats = 0,
            ConfidencePenaltyStrength = 0.50
        });

        act.Should().NotThrow();
        edges.Should().NotBeNull();
        edges!.Should().OnlyContain(edge =>
            double.IsFinite(edge.Similarity) &&
            edge.Similarity >= 0.0 &&
            edge.Similarity <= 1.0);
    }

    [Fact]
    public void F2Pipeline_Should_StillFindBranchesForHealthyRepeatedPattern()
    {
        var beatTracking = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 16).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 16).ToArray()
        };
        var features = CreateRepeatedFeatureMatrix();
        var beats = BeatFeatureAggregator.AggregateFeatures(beatTracking, features, 22_050);
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.9,
            LookaheadDepth = 2,
            MinJumpDistance = 6,
            LandingOffsetBeats = 0
        });

        edges.Should().NotBeEmpty();
        edges.Should().OnlyContain(edge =>
            double.IsFinite(edge.Similarity) &&
            edge.Similarity >= 0.0 &&
            edge.Similarity <= 1.0);
    }

    [Fact]
    public void F4Pipeline_Should_LimitBranchSourceDensityWithoutKillingGraph()
    {
        var beatTracking = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 64).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 64).ToArray()
        };
        var features = CreateLongRepeatedFeatureMatrix(frameCount: 64);
        var beats = BeatFeatureAggregator.AggregateFeatures(beatTracking, features, 22_050);
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.80,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            MaxBranchesPerBeat = 3,
            LandingOffsetBeats = 0,
            TargetBranchSourceRatio = 0.16,
            MaxBranchSourceRatio = 0.22
        });

        edges.Should().NotBeEmpty();
        edges.Select(edge => edge.FromBeat).Distinct().Should().HaveCountLessThanOrEqualTo(11);
        edges.GroupBy(edge => edge.FromBeat).Should().OnlyContain(group => group.Count() <= 3);
    }

    private static FeatureMatrix CreateRepeatedFeatureMatrix()
    {
        var mfcc = new float[16][];
        var chroma = new float[16][];

        for (var i = 0; i < 16; i++)
        {
            mfcc[i] = [0f, 1f];
            chroma[i] = [0f, 1f];
        }

        for (var i = 0; i < 3; i++)
        {
            var value = i + 1;
            mfcc[i] = [value, 0f];
            chroma[i] = [value, 0f];
            mfcc[i + 8] = [value, 0f];
            chroma[i + 8] = [value, 0f];
        }

        return new FeatureMatrix
        {
            Mfcc = mfcc,
            Chroma = chroma,
            SpectralFlux = new float[16],
            Rms = new float[16],
            HopLengthSamples = 11_025,
            FrameSizeSamples = 2048
        };
    }

    private static TrackAnalysis CreateAnalysis(IReadOnlyList<Beat> beats)
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "track.wav",
                DurationSeconds = 8.0,
                SampleRate = 22_050,
                Tempo = 120.0,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = beats,
            Bars = [],
            Tatums = [],
            Sections = [],
            Ai = new AiAnalysisData
            {
                ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
                ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
                SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
                EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
                BeatEmbeddings = beats
                    .Select(beat => new AiBeatEmbedding
                    {
                        BeatIndex = beat.Index,
                        Vector = beat.Index == 8 ? [0.0f, 1.0f] : [1.0f, 0.0f]
                    })
                    .ToArray()
            }
        };
    }

    private static FeatureMatrix CreateLongRepeatedFeatureMatrix(int frameCount)
    {
        var mfcc = new float[frameCount][];
        var chroma = new float[frameCount][];
        var rms = new float[frameCount];
        var flux = new float[frameCount];

        for (var i = 0; i < frameCount; i++)
        {
            var section = i % 8;
            mfcc[i] = [section + 1f, 0f];
            chroma[i] = [section + 1f, 0f];
            rms[i] = 1.0f;
            flux[i] = 0.5f;
        }

        return new FeatureMatrix
        {
            Mfcc = mfcc,
            Chroma = chroma,
            SpectralFlux = flux,
            Rms = rms,
            HopLengthSamples = 11_025,
            FrameSizeSamples = 2048
        };
    }

    private static Beat CopyBeat(Beat beat, double duration, double confidence)
    {
        return new Beat
        {
            Index = beat.Index,
            Start = beat.Start,
            Duration = duration,
            Confidence = confidence,
            Timbre = beat.Timbre,
            Pitches = beat.Pitches,
            Loudness = beat.Loudness,
            BarPosition = beat.BarPosition
        };
    }
}
