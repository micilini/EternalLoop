using System;
using System.Linq;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;
using Xunit;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Hybrid;

public sealed class BeatGridHybridDownbeatSanitizationIntegrationTests
{
    private sealed class MockBeatTracker : IBeatTracker
    {
        private readonly BeatTrackingResult _result;

        public MockBeatTracker(BeatTrackingResult result)
        {
            _result = result;
        }

        public BeatTrackingResult Track(LoadedAudio audio, FeatureMatrix features, BeatTrackingOptions options)
        {
            return _result;
        }
    }

    private static FeatureMatrix CreateDummyFeatures() => new()
    {
        Mfcc = [],
        Chroma = [],
        SpectralFlux = [],
        Rms = [],
        FrameSizeSamples = 2048,
        HopLengthSamples = 512,
        SampleRate = 44100
    };

    [Fact]
    public void Shadow_accepts_advisor_candidate_with_sanitized_downbeats()
    {
        var legacyResult = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            DownbeatTimes = [0.0, 1.0],
            UsedAiProvider = false,
            UsedBuiltInProvider = true
        };

        var advisorResult = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.95, 0.95, 0.95, 0.95],
            DownbeatTimes = [],
            ProviderWarnings = ["beat-this-warning:downbeats-discarded:not-aligned-to-beat:0.9"],
            DownbeatSanitized = true,
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };

        var builtInTracker = new MockBeatTracker(legacyResult);
        var beatThisTracker = new MockBeatTracker(advisorResult);

        var guardrails = new BeatGridGuardrails();
        var selector = new BeatTrackerSelector(builtInTracker, beatThisTracker, guardrails);

        var audio = new LoadedAudio(new float[44100 * 4], 44100, 4.0, "hash", "song.wav", "song.wav");
        var features = CreateDummyFeatures();
        var options = new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Shadow
        };

        var result = selector.Track(audio, features, options);

        result.ShadowDiagnostics.Should().NotBeNull();
        result.ShadowDiagnostics!.Status.Should().Be("succeeded");
        result.ShadowDiagnostics.RejectionReason.Should().BeNull();
        result.ShadowDiagnostics.FailureReason.Should().BeNull();
        result.CandidateSet.Should().NotBeNull();
        result.CandidateSet!.Advisor.Should().NotBeNull();
        result.CandidateSet.Advisor!.Quality.RejectionReason.Should().BeNull();
        result.CandidateSet.Advisor.DownbeatTimes.Should().BeEmpty();
    }

    [Fact]
    public void Hybrid_does_not_fallback_due_to_downbeat_sanitization()
    {
        var legacyResult = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.9, 0.9, 0.9, 0.9],
            DownbeatTimes = [0.0, 1.0],
            UsedAiProvider = false,
            UsedBuiltInProvider = true
        };

        var advisorResult = new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [0.95, 0.95, 0.95, 0.95],
            DownbeatTimes = [],
            ProviderWarnings = ["beat-this-warning:downbeats-discarded:not-aligned-to-beat:0.9"],
            DownbeatSanitized = true,
            UsedAiProvider = true,
            UsedBuiltInProvider = false
        };

        var builtInTracker = new MockBeatTracker(legacyResult);
        var beatThisTracker = new MockBeatTracker(advisorResult);

        var guardrails = new BeatGridGuardrails();
        var selector = new BeatTrackerSelector(builtInTracker, beatThisTracker, guardrails);

        var audio = new LoadedAudio(new float[44100 * 4], 44100, 4.0, "hash", "song.wav", "song.wav");
        var features = CreateDummyFeatures();
        var options = new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Hybrid
        };

        var result = selector.Track(audio, features, options);

        result.CandidateSet.Should().NotBeNull();
        result.CandidateSet!.Advisor.Should().NotBeNull();
        result.CandidateSet.Advisor!.Quality.RejectionReason.Should().NotContain("downbeat-not-aligned-to-beat");
        result.CandidateSet.Advisor.DownbeatTimes.Should().BeEmpty();
        result.FallbackReason.Should().NotContain("downbeat-not-aligned-to-beat");
    }
}
