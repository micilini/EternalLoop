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
}
