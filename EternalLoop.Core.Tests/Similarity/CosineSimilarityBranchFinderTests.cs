using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.Similarity;
using FluentAssertions;

namespace EternalLoop.Core.Tests.Similarity;

public sealed class CosineSimilarityBranchFinderTests
{
    [Fact]
    public void FindBranches_Should_ReturnEmpty_WhenNoBeats()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches([], new BranchFindingOptions());

        edges.Should().BeEmpty();
    }

    [Fact]
    public void FindBranches_Should_ReturnEmpty_WhenNotEnoughBeatsForLookahead()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateIdenticalBeats(3), new BranchFindingOptions { LookaheadDepth = 3 });

        edges.Should().BeEmpty();
    }

    [Fact]
    public void FindBranches_Should_CreateEdge_ForSimilarDistantBeats()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateRepeatedBeats(), new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 2,
            MinJumpDistance = 4
        });

        edges.Should().Contain(edge => edge.FromBeat == 0 && edge.ToBeat == 9);
    }

    [Fact]
    public void FindBranches_Should_NotCreateEdge_ForNearbyBeats()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateRepeatedBeats(), new BranchFindingOptions
        {
            SimilarityThreshold = 0.0,
            LookaheadDepth = 0,
            MinJumpDistance = 4
        });

        edges.Should().NotContain(edge => Math.Abs(edge.FromBeat - edge.ToBeat) < 4);
    }

    [Fact]
    public void FindBranches_Should_RespectLookaheadDepth()
    {
        var beats = CreateRepeatedBeats();
        beats[9] = CreateBeat(9, [0f, 1f], [0f, 1f]);
        beats[10] = CreateBeat(10, [0f, 1f], [0f, 1f]);
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 2,
            MinJumpDistance = 4
        });

        edges.Should().NotContain(edge => edge.FromBeat == 0 && edge.ToBeat == 9);
    }

    [Fact]
    public void FindBranches_Should_LimitMaxBranchesPerBeat()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateIdenticalBeats(12), new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 0,
            MinJumpDistance = 2,
            MaxBranchesPerBeat = 1
        });

        edges.GroupBy(edge => edge.FromBeat).Should().OnlyContain(group => group.Count() <= 1);
    }

    [Fact]
    public void FindBranches_Should_OrderEdgesByFromBeatThenSimilarity()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateIdenticalBeats(8), new BranchFindingOptions
        {
            SimilarityThreshold = 0.0,
            LookaheadDepth = 0,
            MinJumpDistance = 2,
            MaxBranchesPerBeat = 2
        });

        edges.Should().BeInAscendingOrder(edge => edge.FromBeat);
    }

    [Fact]
    public void FindBranches_Should_ClampInvalidThreshold()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateRepeatedBeats(), new BranchFindingOptions
        {
            SimilarityThreshold = 2.0,
            LookaheadDepth = 2,
            MinJumpDistance = 4
        });

        edges.Should().Contain(edge => edge.FromBeat == 0 && edge.ToBeat == 9);
    }

    [Fact]
    public void FindBranches_Should_UseAdaptiveThreshold_WhenFixedThresholdReturnsNoEdges()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateRepeatedBeats(), new BranchFindingOptions
        {
            SimilarityThreshold = 1.0,
            LookaheadDepth = 2,
            MinJumpDistance = 4
        });

        edges.Should().NotBeEmpty();
    }

    [Fact]
    public void FindBranches_Should_NotReturnSelfEdges()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateIdenticalBeats(8), new BranchFindingOptions
        {
            SimilarityThreshold = 0.0,
            LookaheadDepth = 0,
            MinJumpDistance = 1
        });

        edges.Should().NotContain(edge => edge.FromBeat == edge.ToBeat);
    }

    [Fact]
    public void FindBranches_Should_LandAfterMatchingAnchor_ToAvoidRepeatingCurrentBeat()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateRepeatedBeats(), new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 2,
            MinJumpDistance = 4,
            LandingOffsetBeats = 1
        });

        edges.Should().Contain(edge => edge.FromBeat == 0 && edge.ToBeat == 9);
        edges.Should().NotContain(edge => edge.FromBeat == 0 && edge.ToBeat == 8);
    }

    [Fact]
    public void FindBranches_Should_AllowLegacyAnchorLanding_WhenLandingOffsetIsZero()
    {
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(CreateRepeatedBeats(), new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 2,
            MinJumpDistance = 4,
            LandingOffsetBeats = 0
        });

        edges.Should().Contain(edge => edge.FromBeat == 0 && edge.ToBeat == 8);
    }

    [Fact]
    public void FindBranches_Should_NotCreateLandingPastEndOfTrack()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateRepeatedBeats(count: 10);

        beats[0] = CreateBeat(0, [1f, 0f], [1f, 0f]);
        beats[8] = CreateBeat(8, [1f, 0f], [1f, 0f]);
        beats[9] = CreateBeat(9, [0.8f, 0.2f], [0.7f, 0.3f]);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 1,
            MinJumpDistance = 4,
            LandingOffsetBeats = 2
        });

        edges.Should().NotContain(edge => edge.ToBeat >= beats.Length);
    }

    [Fact]
    public void FindBranches_Should_ReturnReasonableEdgeCount_OnPopMusicLikeFeatures()
    {
        var beats = BuildPopMusicLikeBeats(count: 80, sections: 3);
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.82,
            LookaheadDepth = 3,
            MinJumpDistance = 16,
            MaxBranchesPerBeat = 5,
            LandingOffsetBeats = 1
        });

        edges.Should().HaveCountGreaterThan(
            20,
            "pop music with repeating sections should produce more than 20 branches after the AdaptiveThresholdSelector fix");
    }

    [Fact]
    public void FindBranches_Should_RejectCandidate_WhenLandingContinuationDoesNotMatch()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(16);

        beats[0] = CreateBeat(0, [1f, 0f], [1f, 0f], barPosition: [0f, 1f]);
        beats[1] = CreateBeat(1, [0.8f, 0.2f], [0.8f, 0.2f], barPosition: [1f, 0f]);
        beats[2] = CreateBeat(2, [0.6f, 0.4f], [0.6f, 0.4f], barPosition: [0f, -1f]);
        beats[8] = CreateBeat(8, [1f, 0f], [1f, 0f], barPosition: [0f, 1f]);
        beats[9] = CreateBeat(9, [0f, 1f], [0f, 1f], barPosition: [1f, 0f]);
        beats[10] = CreateBeat(10, [0f, 1f], [0f, 1f], barPosition: [0f, -1f]);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.90,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 2,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            LandingOffsetBeats = 1,
            MaxBranchesPerBeat = 5
        });

        edges.Should().NotContain(edge => edge.FromBeat == 0 && edge.ToBeat == 9);
    }

    [Fact]
    public void FindBranches_Should_AcceptCandidate_WhenLandingContinuationMatches()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(16);

        beats[0] = CreateBeat(0, [1f, 0f], [1f, 0f], barPosition: [0f, 1f]);
        beats[1] = CreateBeat(1, [0.8f, 0.2f], [0.8f, 0.2f], barPosition: [1f, 0f]);
        beats[2] = CreateBeat(2, [0.6f, 0.4f], [0.6f, 0.4f], barPosition: [0f, -1f]);
        beats[8] = CreateBeat(8, [1f, 0f], [1f, 0f], barPosition: [0f, 1f]);
        beats[9] = CreateBeat(9, [0.8f, 0.2f], [0.8f, 0.2f], barPosition: [1f, 0f]);
        beats[10] = CreateBeat(10, [0.6f, 0.4f], [0.6f, 0.4f], barPosition: [0f, -1f]);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.90,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 2,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            LandingOffsetBeats = 1,
            MaxBranchesPerBeat = 5
        });

        edges.Should().Contain(edge => edge.FromBeat == 0 && edge.ToBeat == 9);
    }

    [Fact]
    public void FindBranches_Should_KeepPopMusicLikeBranchCountControlled()
    {
        var beats = BuildPopMusicLikeBeats(count: 160, sections: 4);
        var finder = new CosineSimilarityBranchFinder();

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.86,
            LookaheadDepth = 4,
            ContinuationLookaheadDepth = 6,
            ContinuationThresholdMargin = 0.02,
            MinJumpDistance = 20,
            MaxBranchesPerBeat = 3,
            LandingOffsetBeats = 1
        });

        edges.Should().NotBeEmpty();
        edges.Count.Should().BeLessThanOrEqualTo(650);
    }

    [Fact]
    public void FindBranches_WithTrackAnalysis_Should_MatchClassic_WhenAiDisabled()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(16);
        var analysis = CreateAnalysis(beats, CreateRejectingAiData(beats));
        var options = CreateAiOptions(useAiSimilarity: false);

        var classic = finder.FindBranches(beats, options);
        var hybrid = finder.FindBranches(analysis, options);

        hybrid.Select(edge => (edge.FromBeat, edge.ToBeat)).Should().Equal(classic.Select(edge => (edge.FromBeat, edge.ToBeat)));
    }

    [Fact]
    public void FindBranches_WithTrackAnalysis_Should_MatchClassic_WhenAiDataIsMissing()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(16);
        var analysis = CreateAnalysis(beats, ai: null);
        var options = CreateAiOptions(useAiSimilarity: true);

        var classic = finder.FindBranches(beats, options);
        var hybrid = finder.FindBranches(analysis, options);

        hybrid.Select(edge => (edge.FromBeat, edge.ToBeat)).Should().Equal(classic.Select(edge => (edge.FromBeat, edge.ToBeat)));
    }

    [Fact]
    public void FindBranches_WithTrackAnalysis_Should_ProduceNoMoreEdgesThanClassic_WhenAiEnabled()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(24);
        var options = CreateAiOptions(useAiSimilarity: true);
        var classic = finder.FindBranches(beats, options);
        var hybrid = finder.FindBranches(CreateAnalysis(beats, CreateAiData(beats, compatible: false)), options);

        hybrid.Count.Should().BeLessThanOrEqualTo(classic.Count);
    }

    [Fact]
    public void FindBranches_WithTrackAnalysis_Should_RejectAiIncompatibleBranch()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(16);
        var analysis = CreateAnalysis(beats, CreateRejectingAiData(beats));

        var edges = finder.FindBranches(analysis, CreateAiOptions(useAiSimilarity: true));

        edges.Should().BeEmpty();
    }

    [Fact]
    public void FindBranches_WithTrackAnalysis_Should_StillProduceEdges_WhenAiCompatible()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(16);
        var analysis = CreateAnalysis(beats, CreateAiData(beats, compatible: true));

        var edges = finder.FindBranches(analysis, CreateAiOptions(useAiSimilarity: true));

        edges.Should().NotBeEmpty();
    }

    [Fact]
    public void FindBranches_WithTrackAnalysis_Should_NotLetAdaptiveThresholdReviveRejectedAiPairs()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(24);
        var analysis = CreateAnalysis(beats, CreateRejectingAiData(beats));
        var options = new BranchFindingOptions
        {
            UseAiSimilarity = true,
            SimilarityThreshold = 1.0,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 1
        };

        var edges = finder.FindBranches(analysis, options);

        edges.Should().BeEmpty();
    }

    [Fact]
    public void FindBranches_WithBeatsOverload_Should_RemainClassic()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(16);

        var edges = finder.FindBranches(beats, CreateAiOptions(useAiSimilarity: true));

        edges.Should().NotBeEmpty();
    }

    [Fact]
    public void FindBranches_Should_FilterDurationMismatch_WhenDurationGateEnabled()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateRepeatedBeats();

        beats[8] = CreateBeat(8, [1f, 0f], [1f, 0f], duration: 0.25);
        beats[9] = CreateBeat(9, [0.8f, 0.2f], [0.7f, 0.3f], duration: 0.25);
        beats[10] = CreateBeat(10, [0.6f, 0.4f], [0.5f, 0.5f], duration: 0.25);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 2,
            ContinuationLookaheadDepth = 2,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            LandingOffsetBeats = 0,
            DurationRejectionRatio = 0.80
        });

        edges.Should().NotContain(edge => edge.FromBeat == 0 && edge.ToBeat == 8);
    }

    [Fact]
    public void FindBranches_Should_PenalizeLowConfidence_WhenConfidencePenaltyEnabled()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateRepeatedBeats();

        beats[8] = CreateBeat(8, [1f, 0f], [1f, 0f], confidence: 0.10);
        beats[9] = CreateBeat(9, [0.8f, 0.2f], [0.7f, 0.3f], confidence: 0.10);
        beats[10] = CreateBeat(10, [0.6f, 0.4f], [0.5f, 0.5f], confidence: 0.10);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.80,
            LookaheadDepth = 2,
            ContinuationLookaheadDepth = 2,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            LandingOffsetBeats = 0,
            ConfidencePenaltyStart = 0.50,
            ConfidenceRejectionThreshold = 0.25,
            ConfidencePenaltyStrength = 0.50
        });

        edges.Should().NotContain(edge => edge.FromBeat == 0 && edge.ToBeat == 8);
    }

    [Fact]
    public void FindBranches_Should_PreservePreviousBehavior_WhenBranchQualityFiltersDisabled()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateRepeatedBeats();

        beats[8] = CreateBeat(8, [1f, 0f], [1f, 0f], duration: 0.25, confidence: 0.10);
        beats[9] = CreateBeat(9, [0.8f, 0.2f], [0.7f, 0.3f], duration: 0.25, confidence: 0.10);
        beats[10] = CreateBeat(10, [0.6f, 0.4f], [0.5f, 0.5f], duration: 0.25, confidence: 0.10);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.95,
            LookaheadDepth = 2,
            ContinuationLookaheadDepth = 2,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            LandingOffsetBeats = 0,
            UseDurationSimilarityGate = false,
            UseConfidencePenalty = false
        });

        edges.Should().Contain(edge => edge.FromBeat == 0 && edge.ToBeat == 8);
    }

    [Fact]
    public void FindBranches_Should_LimitSourceDensity_InBalancedMode()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(80);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.0,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 1,
            MaxBranchesPerBeat = 3,
            LandingOffsetBeats = 0,
            TargetBranchSourceRatio = 0.16,
            MaxBranchSourceRatio = 0.22,
            UseDurationSimilarityGate = false,
            UseConfidencePenalty = false,
            MetricPositionMode = MetricPositionMode.Disabled,
            UseAiSimilarity = false
        });

        edges.Select(edge => edge.FromBeat).Distinct().Should().HaveCountLessThanOrEqualTo(13);
    }

    [Fact]
    public void FindBranches_Should_KeepConservativeSourceDensityBelowWild()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(100);
        var conservativeOptions = new BranchFindingOptions
        {
            SimilarityThreshold = 0.0,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 1,
            MaxBranchesPerBeat = 3,
            LandingOffsetBeats = 0,
            UseDurationSimilarityGate = false,
            UseConfidencePenalty = false,
            MetricPositionMode = MetricPositionMode.Disabled,
            UseAiSimilarity = false,
            TargetBranchSourceRatio = 0.10,
            MaxBranchSourceRatio = 0.14
        };
        var wildOptions = new BranchFindingOptions
        {
            SimilarityThreshold = 0.0,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 1,
            MaxBranchesPerBeat = 3,
            LandingOffsetBeats = 0,
            UseDurationSimilarityGate = false,
            UseConfidencePenalty = false,
            MetricPositionMode = MetricPositionMode.Disabled,
            UseAiSimilarity = false,
            TargetBranchSourceRatio = 0.25,
            MaxBranchSourceRatio = 0.34
        };

        var conservativeEdges = finder.FindBranches(beats, conservativeOptions);
        var wildEdges = finder.FindBranches(beats, wildOptions);

        conservativeEdges.Select(edge => edge.FromBeat).Distinct().Count()
            .Should().BeLessThan(wildEdges.Select(edge => edge.FromBeat).Distinct().Count());
    }

    [Fact]
    public void FindBranches_Should_StillProduceEdgesAfterSourceDensityLimit()
    {
        var finder = new CosineSimilarityBranchFinder();
        var beats = CreateIdenticalBeats(64);

        var edges = finder.FindBranches(beats, new BranchFindingOptions
        {
            SimilarityThreshold = 0.90,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            MaxBranchesPerBeat = 2,
            LandingOffsetBeats = 0,
            TargetBranchSourceRatio = 0.16,
            MaxBranchSourceRatio = 0.22,
            UseDurationSimilarityGate = false,
            UseConfidencePenalty = false,
            MetricPositionMode = MetricPositionMode.Disabled,
            UseAiSimilarity = false
        });

        edges.Should().NotBeEmpty();
        edges.Select(edge => edge.FromBeat).Distinct().Should().HaveCountLessThanOrEqualTo(11);
    }

    private static Beat[] CreateRepeatedBeats(int count = 20)
    {
        var beats = Enumerable.Range(0, count)
            .Select(i => CreateBeat(i, [0f, 1f], [0f, 1f]))
            .ToArray();

        beats[0] = CreateBeat(0, [1f, 0f], [1f, 0f]);
        beats[1] = CreateBeat(1, [0.8f, 0.2f], [0.7f, 0.3f]);
        beats[2] = CreateBeat(2, [0.6f, 0.4f], [0.5f, 0.5f]);

        if (count > 8)
        {
            beats[8] = CreateBeat(8, [1f, 0f], [1f, 0f]);
        }

        if (count > 9)
        {
            beats[9] = CreateBeat(9, [0.8f, 0.2f], [0.7f, 0.3f]);
        }

        if (count > 10)
        {
            beats[10] = CreateBeat(10, [0.6f, 0.4f], [0.5f, 0.5f]);
        }

        return beats;
    }

    private static Beat[] CreateIdenticalBeats(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateBeat(i, [1f, 0f], [1f, 0f]))
            .ToArray();
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
            Loudness = loudness ?? [1f, 1f, 1f],
            BarPosition = barPosition ?? [1f, 0f]
        };
    }

    private static IReadOnlyList<Beat> BuildPopMusicLikeBeats(int count, int sections)
    {
        var beats = new List<Beat>(count);

        for (var i = 0; i < count; i++)
        {
            var sectionId = i % sections;
            var timbre = new float[13];
            var chroma = new float[12];

            for (var d = 0; d < 13; d++)
            {
                timbre[d] = d % sections == sectionId ? 1f : 0.1f;
            }

            for (var d = 0; d < 12; d++)
            {
                chroma[d] = d % sections == sectionId ? 1f : 0.1f;
            }

            beats.Add(new Beat
            {
                Index = i,
                Start = i * 0.5,
                Duration = 0.5,
                Confidence = 1.0,
                Timbre = timbre,
                Pitches = chroma,
                Loudness = [1f, 1f, 1f],
                BarPosition = [1f, 0f]
            });
        }

        return beats;
    }

    private static BranchFindingOptions CreateAiOptions(bool useAiSimilarity)
    {
        return new BranchFindingOptions
        {
            UseAiSimilarity = useAiSimilarity,
            SimilarityThreshold = 0.90,
            LookaheadDepth = 0,
            ContinuationLookaheadDepth = 0,
            ContinuationThresholdMargin = 0.0,
            MinJumpDistance = 4,
            MaxBranchesPerBeat = 3,
            LandingOffsetBeats = 0,
            TimbreWeight = 1.0,
            PitchWeight = 0.0,
            LoudnessWeight = 0.0,
            BarPositionWeight = 0.0,
            AiRejectionThreshold = 0.58,
            AiPenaltyStartThreshold = 0.72,
            AiPenaltyStrength = 0.22
        };
    }

    private static TrackAnalysis CreateAnalysis(IReadOnlyList<Beat> beats, AiAnalysisData? ai)
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "track.wav",
                DurationSeconds = beats.Count * 0.5,
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
            Ai = ai
        };
    }

    private static AiAnalysisData CreateAiData(IReadOnlyList<Beat> beats, bool compatible)
    {
        return new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings = beats
                .Select(beat => new AiBeatEmbedding
                {
                    BeatIndex = beat.Index,
                    Vector = compatible || beat.Index % 2 == 0 ? [1.0f, 0.0f] : [0.0f, 1.0f]
                })
                .ToArray()
        };
    }

    private static AiAnalysisData CreateRejectingAiData(IReadOnlyList<Beat> beats)
    {
        return new AiAnalysisData
        {
            ModelId = AiModelDefaultValues.DiscogsEffNetModelId,
            ModelVersion = AiModelDefaultValues.DiscogsEffNetVersion,
            SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            BeatEmbeddings = beats
                .Select((beat, index) =>
                {
                    var vector = new float[beats.Count];
                    vector[index] = 1.0f;
                    return new AiBeatEmbedding
                    {
                        BeatIndex = beat.Index,
                        Vector = vector
                    };
                })
                .ToArray()
        };
    }
}
