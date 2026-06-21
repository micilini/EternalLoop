using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking;

public sealed class BeatTrackerSelectorTests
{
    [Fact]
    public void BuiltIn_calls_built_in_tracker()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult());
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BuiltIn
        });

        builtIn.CallCount.Should().Be(1);
        beatThis.CallCount.Should().Be(0);
        result.Should().BeSameAs(builtIn.Result);
    }

    [Fact]
    public void Auto_without_beat_this_tracker_calls_built_in_tracker()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var selector = new BeatTrackerSelector(builtIn);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Auto
        });

        builtIn.CallCount.Should().Be(1);
        result.Should().BeSameAs(builtIn.Result);
    }

    [Fact]
    public void Auto_with_beat_this_tracker_returns_built_in_conservative()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult());
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Auto
        });

        beatThis.CallCount.Should().Be(0);
        builtIn.CallCount.Should().Be(1);
        result.Should().BeSameAs(builtIn.Result);
        result.UsedAiProvider.Should().BeFalse();
        result.UsedBuiltInProvider.Should().BeTrue();
    }

    [Fact]
    public void Shadow_without_ai_tracker_returns_built_in_with_not_configured_diagnostics()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var selector = new BeatTrackerSelector(builtIn);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Shadow
        });

        builtIn.CallCount.Should().Be(1);
        result.BeatTimes.Should().Equal(builtIn.Result.BeatTimes);
        result.ProviderName.Should().Be("built-in");
        result.UsedAiProvider.Should().BeFalse();
        result.UsedBuiltInProvider.Should().BeTrue();
        result.UsedFallbackProvider.Should().BeFalse();
        result.ShadowDiagnostics.Should().NotBeNull();
        result.ShadowDiagnostics!.Enabled.Should().BeTrue();
        result.ShadowDiagnostics.Status.Should().Be("not-configured");
        result.BeatGridMode.Should().Be("built-in-test+shadow");
        result.CandidateSet.Should().NotBeNull();
        result.CandidateSet!.All.Should().ContainSingle();
        result.CandidateSet.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
    }

    [Fact]
    public void Shadow_with_ai_tracker_returns_built_in_final_and_shadow_succeeded()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult());
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Shadow
        });

        builtIn.CallCount.Should().Be(1);
        beatThis.CallCount.Should().Be(1);
        result.BeatTimes.Should().Equal(builtIn.Result.BeatTimes);
        result.Confidences.Should().Equal(builtIn.Result.Confidences);
        result.ProviderName.Should().Be("built-in");
        result.UsedAiProvider.Should().BeFalse();
        result.UsedBuiltInProvider.Should().BeTrue();
        result.UsedFallbackProvider.Should().BeFalse();
        result.FallbackReason.Should().BeNull();
        result.ShadowDiagnostics.Should().NotBeNull();
        result.ShadowDiagnostics!.Status.Should().Be("succeeded");
        result.ShadowDiagnostics.AdvisorProvider.Should().Be("beat-this");
        result.ShadowDiagnostics.LegacyBeatCount.Should().Be(4);
        result.ShadowDiagnostics.AdvisorBeatCount.Should().Be(4);
        result.ShadowDiagnostics.AgreementF1_70Ms.Should().Be(1.0);
        result.CandidateSet.Should().NotBeNull();
        result.CandidateSet!.Legacy.Should().NotBeNull();
        result.CandidateSet.Advisor.Should().NotBeNull();
        result.CandidateSet.Selected.Should().BeSameAs(result.CandidateSet.Legacy);
        result.CandidateSet.Diagnostics.AdvisorAcceptedAsCandidate.Should().BeTrue();
        result.CandidateSet.AgreementConfidence.Should().NotBeNull();
        result.CandidateSet.WeakWindows.Should().NotBeNull();
        result.CandidateSet.WeakWindowCorrections.Should().NotBeNull();
        result.ShadowDiagnostics.CandidateSetEnabled.Should().BeTrue();
        result.ShadowDiagnostics.SelectedCandidateId.Should().Be("legacy");
        result.ShadowDiagnostics.AdvisorCandidateId.Should().Be("beat-this-advisor");
    }

    [Fact]
    public void Shadow_with_ai_guardrail_failure_returns_built_in_final_and_shadow_rejected()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult(
            beatTimes: [0.0],
            confidences: [0.9]));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Shadow
        });

        builtIn.CallCount.Should().Be(1);
        beatThis.CallCount.Should().Be(1);
        result.BeatTimes.Should().Equal(builtIn.Result.BeatTimes);
        result.UsedAiProvider.Should().BeFalse();
        result.UsedBuiltInProvider.Should().BeTrue();
        result.UsedFallbackProvider.Should().BeFalse();
        result.ShadowDiagnostics.Should().NotBeNull();
        result.ShadowDiagnostics!.Status.Should().Be("rejected");
        result.ShadowDiagnostics.RejectionReason.Should().StartWith("beat-this-guardrail-rejected:beat-count-too-low");
        result.CandidateSet.Should().NotBeNull();
        result.CandidateSet!.Advisor.Should().NotBeNull();
        result.CandidateSet.Diagnostics.AdvisorAcceptedAsCandidate.Should().BeFalse();
        result.CandidateSet.Diagnostics.AdvisorRejectionReason.Should().StartWith("beat-this-guardrail-rejected:beat-count-too-low");
        result.CandidateSet.AgreementConfidence.Should().NotBeNull();
        result.CandidateSet.AgreementConfidence!.FutureFusionReady.Should().BeFalse();
        result.CandidateSet.WeakWindows.Should().NotBeNull();
        result.CandidateSet.WeakWindows!.FutureCorrectionReady.Should().BeFalse();
        result.CandidateSet.WeakWindowCorrections.Should().NotBeNull();
        result.CandidateSet.WeakWindowCorrections!.ShouldApplyCorrection.Should().BeFalse();
    }

    [Fact]
    public void Shadow_with_ai_exception_returns_built_in_final_and_shadow_failed()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new ThrowingBeatTracker("model exploded");
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Shadow
        });

        builtIn.CallCount.Should().Be(1);
        beatThis.CallCount.Should().Be(1);
        result.BeatTimes.Should().Equal(builtIn.Result.BeatTimes);
        result.UsedAiProvider.Should().BeFalse();
        result.UsedBuiltInProvider.Should().BeTrue();
        result.UsedFallbackProvider.Should().BeFalse();
        result.ShadowDiagnostics.Should().NotBeNull();
        result.ShadowDiagnostics!.Status.Should().Be("failed");
        result.ShadowDiagnostics.FailureReason.Should().Be("beat-this-provider-failed:model exploded");
        result.CandidateSet.Should().NotBeNull();
        result.CandidateSet!.Advisor.Should().BeNull();
        result.CandidateSet.All.Should().ContainSingle();
        result.CandidateSet.Diagnostics.AdvisorRejectionReason.Should().Be("beat-this-provider-failed:model exploded");
        result.CandidateSet.AgreementConfidence.Should().NotBeNull();
        result.CandidateSet.AgreementConfidence!.Status.Should().Be("not-available");
        result.CandidateSet.WeakWindows.Should().NotBeNull();
        result.CandidateSet.WeakWindows!.Status.Should().Be("not-available");
        result.CandidateSet.WeakWindowCorrections.Should().NotBeNull();
        result.CandidateSet.WeakWindowCorrections!.Status.Should().Be("not-available");
    }

    [Fact]
    public void Shadow_candidate_set_does_not_change_final_beat_times()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult(beatTimes: [0.1, 0.6, 1.1, 1.6]));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Shadow
        });

        result.BeatTimes.Should().Equal(builtIn.Result.BeatTimes);
        result.CandidateSet!.Advisor!.BeatTimes.Should().Equal(beatThis.Result.BeatTimes);
        result.CandidateSet.Selected!.BeatTimes.Should().Equal(builtIn.Result.BeatTimes);
    }

    [Fact]
    public void Auto_does_not_create_advisor_candidate()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult());
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.Auto
        });

        result.CandidateSet.Should().BeNull();
        beatThis.CallCount.Should().Be(0);
    }

    [Fact]
    public void BuiltIn_does_not_create_advisor_candidate()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult());
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BuiltIn
        });

        result.CandidateSet.Should().BeNull();
        beatThis.CallCount.Should().Be(0);
    }

    [Fact]
    public void BeatThis_with_fallback_and_no_ai_tracker_calls_built_in_with_fallback_metadata()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var selector = new BeatTrackerSelector(builtIn);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.FallbackToBuiltIn
        });

        builtIn.CallCount.Should().Be(1);
        result.UsedAiProvider.Should().BeFalse();
        result.UsedBuiltInProvider.Should().BeTrue();
        result.UsedFallbackProvider.Should().BeTrue();
        result.FallbackReason.Should().Be("beat-this-provider-not-configured");
    }

    [Fact]
    public void BeatThis_explicit_still_returns_ai_when_guardrails_pass()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult());
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis
        });

        builtIn.CallCount.Should().Be(0);
        beatThis.CallCount.Should().Be(1);
        result.Should().BeSameAs(beatThis.Result);
        result.UsedAiProvider.Should().BeTrue();
    }

    [Fact]
    public void BeatThis_with_fail_and_no_ai_tracker_throws_clear_error()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var selector = new BeatTrackerSelector(builtIn);

        var act = () => selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.Fail
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Beat This provider is not configured yet.");
        builtIn.CallCount.Should().Be(0);
    }

    [Fact]
    public void BeatThis_with_guardrail_failure_and_fallback_calls_built_in()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult(
            beatTimes: [0.0],
            confidences: [0.9]));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.FallbackToBuiltIn
        });

        beatThis.CallCount.Should().Be(1);
        builtIn.CallCount.Should().Be(1);
        result.UsedFallbackProvider.Should().BeTrue();
        result.FallbackReason.Should().StartWith("beat-this-guardrail-rejected:beat-count-too-low");
    }

    [Fact]
    public void BeatThis_with_guardrail_failure_and_fail_throws_clear_error()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new RecordingBeatTracker(CreateAiResult(
            beatTimes: [0.0],
            confidences: [0.9]));
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var act = () => selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.Fail
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Beat This result rejected by guardrails: beat-count-too-low:1");
        builtIn.CallCount.Should().Be(0);
    }

    [Fact]
    public void BeatThis_with_exception_and_fallback_calls_built_in()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new ThrowingBeatTracker("model exploded");
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var result = selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.FallbackToBuiltIn
        });

        beatThis.CallCount.Should().Be(1);
        builtIn.CallCount.Should().Be(1);
        result.UsedFallbackProvider.Should().BeTrue();
        result.FallbackReason.Should().Be("beat-this-provider-failed:model exploded");
    }

    [Fact]
    public void BeatThis_with_exception_and_fail_propagates_error()
    {
        var builtIn = new RecordingBeatTracker(CreateBuiltInResult());
        var beatThis = new ThrowingBeatTracker("model exploded");
        var selector = new BeatTrackerSelector(builtIn, beatThis);

        var act = () => selector.Track(CreateAudio(), CreateFeatures(), new BeatTrackingOptions
        {
            BeatProvider = BeatTrackingProviderKind.BeatThis,
            AiFallbackMode = AiFallbackMode.Fail
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("model exploded");
        builtIn.CallCount.Should().Be(0);
    }

    private static BeatTrackingResult CreateBuiltInResult()
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = [0.0, 0.5, 1.0, 1.5],
            Confidences = [1.0, 0.9, 0.8, 0.7],
            ProviderName = "built-in",
            UsedBuiltInProvider = true,
            UsedAiProvider = false,
            BeatGridMode = "built-in-test"
        };
    }

    private static BeatTrackingResult CreateAiResult(
        double[]? beatTimes = null,
        double[]? confidences = null)
    {
        return new BeatTrackingResult
        {
            EstimatedBpm = 120.0,
            BeatTimes = beatTimes ?? [0.0, 0.5, 1.0, 1.5],
            Confidences = confidences ?? [0.9, 0.8, 0.7, 0.6],
            DownbeatTimes = [0.0, 1.0],
            ProviderName = "beat-this",
            UsedAiProvider = true,
            UsedBuiltInProvider = false,
            BeatGridMode = "beat-this-onnx-musical-v1"
        };
    }

    private static LoadedAudio CreateAudio()
    {
        return new LoadedAudio([], 22050, 4.0, "hash", "song.wav", "song.wav");
    }

    private static FeatureMatrix CreateFeatures()
    {
        return new FeatureMatrix
        {
            Mfcc = [],
            Chroma = [],
            SpectralFlux = [],
            Rms = [],
            FrameSizeSamples = 2048,
            HopLengthSamples = 512,
            SampleRate = 22050
        };
    }

    private sealed class RecordingBeatTracker : IBeatTracker
    {
        public RecordingBeatTracker(BeatTrackingResult result)
        {
            Result = result;
        }

        public BeatTrackingResult Result { get; }

        public int CallCount { get; private set; }

        public BeatTrackingResult Track(
            LoadedAudio audio,
            FeatureMatrix features,
            BeatTrackingOptions options)
        {
            CallCount++;
            return Result;
        }
    }

    private sealed class ThrowingBeatTracker : IBeatTracker
    {
        private readonly string _message;

        public ThrowingBeatTracker(string message)
        {
            _message = message;
        }

        public int CallCount { get; private set; }

        public BeatTrackingResult Track(
            LoadedAudio audio,
            FeatureMatrix features,
            BeatTrackingOptions options)
        {
            CallCount++;
            throw new InvalidOperationException(_message);
        }
    }
}
