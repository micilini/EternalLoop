using EternalLoop.Contracts.Models;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class BeatFeatureAggregatorTests
{
    [Fact]
    public void AggregateFeatures_Should_ReturnEmptyList_WhenNoBeatTimes()
    {
        var beats = BeatFeatureAggregator.AggregateFeatures(
            new BeatTrackingResult { EstimatedBpm = 120, BeatTimes = [], Confidences = [] },
            CreateFeatureMatrix(),
            22_050);

        beats.Should().BeEmpty();
    }

    [Fact]
    public void AggregateFeatures_Should_CreateBeatPerBeatTime()
    {
        var beats = BeatFeatureAggregator.AggregateFeatures(CreateBeatTrackingResult(), CreateFeatureMatrix(), 22_050);

        beats.Should().HaveCount(3);
        beats.Select(beat => beat.Index).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void AggregateFeatures_Should_ComputeDurations_FromNextBeat()
    {
        var beats = BeatFeatureAggregator.AggregateFeatures(CreateBeatTrackingResult(), CreateFeatureMatrix(), 22_050);

        beats[0].Duration.Should().Be(0.5);
        beats[1].Duration.Should().Be(0.75);
    }

    [Fact]
    public void AggregateFeatures_Should_UsePreviousDuration_ForLastBeat()
    {
        var beats = BeatFeatureAggregator.AggregateFeatures(CreateBeatTrackingResult(), CreateFeatureMatrix(), 22_050);

        beats[2].Duration.Should().Be(0.75);
    }

    [Fact]
    public void AggregateFeatures_Should_AggregateMfccByMedian()
    {
        var beats = BeatFeatureAggregator.AggregateFeatures(CreateBeatTrackingResult(), CreateFeatureMatrix(), 22_050);

        beats[0].Timbre.Should().Equal(4.5f, 9f);
    }

    [Fact]
    public void AggregateFeatures_Should_AggregateChromaByMedian()
    {
        var beats = BeatFeatureAggregator.AggregateFeatures(CreateBeatTrackingResult(), CreateFeatureMatrix(), 22_050);

        beats[0].Pitches.Should().Equal(5.5f, 4.5f);
    }

    [Fact]
    public void AggregateFeatures_Should_UseZeroConfidence_WhenConfidenceIsMissing()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = [0.0, 0.5],
            Confidences = [0.8]
        };

        var beats = BeatFeatureAggregator.AggregateFeatures(result, CreateFeatureMatrix(), 22_050);

        beats[0].Confidence.Should().Be(0.8);
        beats[1].Confidence.Should().Be(0.0);
    }

    [Fact]
    public void AggregateFeatures_Should_Throw_WhenSampleRateIsInvalid()
    {
        var act = () => BeatFeatureAggregator.AggregateFeatures(CreateBeatTrackingResult(), CreateFeatureMatrix(), 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AggregateFeatures_Should_Throw_WhenHopLengthIsInvalid()
    {
        var features = new FeatureMatrix
        {
            Mfcc = [],
            Chroma = [],
            SpectralFlux = [],
            Rms = [],
            HopLengthSamples = 0,
            FrameSizeSamples = 2048
        };

        var act = () => BeatFeatureAggregator.AggregateFeatures(CreateBeatTrackingResult(), features, 22_050);

        act.Should().Throw<ArgumentException>();
    }

    private static BeatTrackingResult CreateBeatTrackingResult()
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = [0.0, 0.5, 1.25],
            Confidences = [0.9, 0.8, 0.7]
        };
    }

    private static FeatureMatrix CreateFeatureMatrix()
    {
        return new FeatureMatrix
        {
            Mfcc = Enumerable.Range(0, 10)
                .Select(i => new[] { (float)i, (float)(i * 2) })
                .ToArray(),
            Chroma = Enumerable.Range(0, 10)
                .Select(i => new[] { (float)(10 - i), (float)i })
                .ToArray(),
            SpectralFlux = new float[10],
            Rms = new float[10],
            HopLengthSamples = 512,
            FrameSizeSamples = 2048
        };
    }

    [Fact]
    public void AggregateFeatures_Should_PopulateLoudnessVectorOfThreeDimensions()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = [0.0, 0.5, 1.0],
            Confidences = [1.0, 1.0, 1.0]
        };

        var features = BuildFeatureMatrix(frameCount: 100);

        var beats = BeatFeatureAggregator.AggregateFeatures(result, features, sampleRate: 22_050);

        beats.Should().HaveCount(3);
        beats.Should().AllSatisfy(beat =>
        {
            beat.Loudness.Should().NotBeNull();
            beat.Loudness.Length.Should().Be(3);
        });
    }

    [Fact]
    public void AggregateFeatures_Should_ProduceZScoredLoudness_WithApproximatelyZeroMeanAcrossBeats()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 20).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 20).ToArray()
        };

        var features = BuildFeatureMatrixWithVaryingRms(frameCount: 500);

        var beats = BeatFeatureAggregator.AggregateFeatures(result, features, sampleRate: 22_050);

        beats.Average(beat => beat.Loudness[0]).Should().BeApproximately(0f, 0.01f);
        beats.Average(beat => beat.Loudness[1]).Should().BeApproximately(0f, 0.01f);
        beats.Average(beat => beat.Loudness[2]).Should().BeApproximately(0f, 0.01f);
    }

    [Fact]
    public void AggregateFeatures_Should_ProduceZeroLoudness_ForUniformRmsTrack()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 10).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 10).ToArray()
        };

        var features = BuildFeatureMatrixWithUniformRms(frameCount: 250, rmsValue: 0.5f);

        var beats = BeatFeatureAggregator.AggregateFeatures(result, features, sampleRate: 22_050);

        beats.Should().AllSatisfy(beat =>
        {
            beat.Loudness[0].Should().BeApproximately(0f, 0.01f);
            beat.Loudness[1].Should().BeApproximately(0f, 0.01f);
            beat.Loudness[2].Should().BeApproximately(0f, 0.01f);
        });
    }

    [Fact]
    public void AggregateFeatures_Should_PopulateBarPositionVectorOfTwoDimensions()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [1.0, 1.0, 1.0, 1.0]
        };

        var features = BuildFeatureMatrix(frameCount: 100);

        var beats = BeatFeatureAggregator.AggregateFeatures(
            result,
            features,
            sampleRate: 22_050,
            timeSignature: 4);

        beats.Should().HaveCount(4);
        beats.Should().AllSatisfy(beat =>
        {
            beat.BarPosition.Should().NotBeNull();
            beat.BarPosition.Length.Should().Be(2);
        });
    }

    [Fact]
    public void AggregateFeatures_Should_ProduceIdenticalBarPosition_ForBeatsAtSameMetricPosition()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = Enumerable.Range(0, 8).Select(i => i * 0.5).ToArray(),
            Confidences = Enumerable.Repeat(1.0, 8).ToArray()
        };

        var features = BuildFeatureMatrix(frameCount: 200);

        var beats = BeatFeatureAggregator.AggregateFeatures(
            result,
            features,
            sampleRate: 22_050,
            timeSignature: 4);

        beats[0].BarPosition.Should().BeEquivalentTo(beats[4].BarPosition);
        beats[1].BarPosition.Should().BeEquivalentTo(beats[5].BarPosition);
        beats[2].BarPosition.Should().BeEquivalentTo(beats[6].BarPosition);
        beats[3].BarPosition.Should().BeEquivalentTo(beats[7].BarPosition);
    }

    [Fact]
    public void AggregateFeatures_Should_ProduceDistinctBarPosition_ForBeatsAtDifferentMetricPositions()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [1.0, 1.0, 1.0, 1.0]
        };

        var features = BuildFeatureMatrix(frameCount: 100);

        var beats = BeatFeatureAggregator.AggregateFeatures(
            result,
            features,
            sampleRate: 22_050,
            timeSignature: 4);

        beats[0].BarPosition.Should().NotBeEquivalentTo(beats[1].BarPosition);
        beats[0].BarPosition.Should().NotBeEquivalentTo(beats[2].BarPosition);
        beats[0].BarPosition.Should().NotBeEquivalentTo(beats[3].BarPosition);
    }

    [Fact]
    public void AggregateFeatures_Should_Throw_WhenTimeSignatureIsZero()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = [0.0, 0.5],
            Confidences = [1.0, 1.0]
        };

        var features = BuildFeatureMatrix(frameCount: 100);

        var act = () => BeatFeatureAggregator.AggregateFeatures(
            result,
            features,
            sampleRate: 22_050,
            timeSignature: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AggregateFeatures_Should_DefaultToFourFour_WhenTimeSignatureIsNotProvided()
    {
        var result = new BeatTrackingResult
        {
            EstimatedBpm = 120,
            BeatTimes = [0.0, 0.5, 1.0, 1.5, 2.0],
            Confidences = [1.0, 1.0, 1.0, 1.0, 1.0]
        };

        var features = BuildFeatureMatrix(frameCount: 200);

        var beats = BeatFeatureAggregator.AggregateFeatures(
            result,
            features,
            sampleRate: 22_050);

        beats[0].BarPosition.Should().BeEquivalentTo(beats[4].BarPosition);
    }

    private static FeatureMatrix BuildFeatureMatrix(int frameCount)
    {
        var mfcc = new float[frameCount][];
        var chroma = new float[frameCount][];

        for (var i = 0; i < frameCount; i++)
        {
            mfcc[i] = new float[13];
            chroma[i] = new float[12];
        }

        return new FeatureMatrix
        {
            Mfcc = mfcc,
            Chroma = chroma,
            SpectralFlux = new float[frameCount],
            Rms = new float[frameCount],
            HopLengthSamples = 512,
            FrameSizeSamples = 2048
        };
    }

    private static FeatureMatrix BuildFeatureMatrixWithVaryingRms(int frameCount)
    {
        var matrix = BuildFeatureMatrix(frameCount);
        var rms = new float[frameCount];

        for (var i = 0; i < frameCount; i++)
        {
            rms[i] = (float)(0.1 + 0.4 * Math.Sin(i * 0.05));
        }

        return new FeatureMatrix
        {
            Mfcc = matrix.Mfcc,
            Chroma = matrix.Chroma,
            SpectralFlux = matrix.SpectralFlux,
            Rms = rms,
            HopLengthSamples = matrix.HopLengthSamples,
            FrameSizeSamples = matrix.FrameSizeSamples
        };
    }

    private static FeatureMatrix BuildFeatureMatrixWithUniformRms(int frameCount, float rmsValue)
    {
        var matrix = BuildFeatureMatrix(frameCount);
        var rms = new float[frameCount];
        Array.Fill(rms, rmsValue);

        return new FeatureMatrix
        {
            Mfcc = matrix.Mfcc,
            Chroma = matrix.Chroma,
            SpectralFlux = matrix.SpectralFlux,
            Rms = rms,
            HopLengthSamples = matrix.HopLengthSamples,
            FrameSizeSamples = matrix.FrameSizeSamples
        };
    }
}
