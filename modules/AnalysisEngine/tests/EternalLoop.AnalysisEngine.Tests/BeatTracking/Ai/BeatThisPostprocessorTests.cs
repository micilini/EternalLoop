using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class BeatThisPostprocessorTests
{
    [Fact]
    public void Postprocess_picks_beat_and_downbeat_peaks()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = [0.1f, 0.9f, 0.1f, 0.1f, 0.85f, 0.1f, 0.1f, 0.8f],
            DownbeatActivations = [0.8f, 0.1f, 0.1f, 0.1f, 0.75f, 0.1f, 0.1f, 0.1f],
            FrameRate = 2.0,
            ValidFrameCount = 8,
            OutputMode = "test"
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.25,
            MinDownbeatSpacingSeconds = 0.25,
            MaxDownbeatSnapDistanceSeconds = 0.01
        });

        var result = postprocessor.Postprocess(inference, availability);

        result.BeatTimes.Should().Equal(0.5, 2.0, 3.5);
        result.DownbeatTimes.Should().Equal(2.0);
        result.Confidences.Should().HaveCount(3);
        result.ProviderName.Should().Be("beat-this");
        result.UsedAiProvider.Should().BeTrue();
        result.UsedBuiltInProvider.Should().BeFalse();
        result.BeatGridMode.Should().Be("beat-this-onnx-musical-v2-full-track");
    }

    [Fact]
    public void Postprocess_converts_logits_to_probabilities()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = [-4.0f, 4.0f, -4.0f, 4.0f],
            DownbeatActivations = [4.0f, -4.0f, -4.0f, -4.0f],
            FrameRate = 2.0,
            ValidFrameCount = 4
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.25,
            MinDownbeatSpacingSeconds = 0.25,
            MaxDownbeatSnapDistanceSeconds = 0.01
        });

        var result = postprocessor.Postprocess(inference, availability);

        result.BeatTimes.Should().Equal(0.5, 1.5);
        result.DownbeatTimes.Should().BeEmpty();
        result.Confidences.Should().OnlyContain(confidence => confidence > 0.9);
    }

    [Fact]
    public void Postprocess_estimates_bpm_from_beat_intervals()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = [0.9f, 0.1f, 0.9f, 0.1f, 0.9f],
            DownbeatActivations = [0.9f, 0.1f, 0.1f, 0.1f, 0.1f],
            FrameRate = 2.0,
            ValidFrameCount = 5
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.25,
            MinDownbeatSpacingSeconds = 0.25
        });

        var result = postprocessor.Postprocess(inference, availability);

        result.EstimatedBpm.Should().BeApproximately(60.0, 1e-9);
    }

    [Fact]
    public void Postprocess_snaps_downbeats_to_nearest_beat_within_tolerance()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = [0.9f, 0.1f, 0.9f, 0.1f, 0.9f, 0.1f, 0.9f],
            DownbeatActivations = [0.1f, 0.85f, 0.1f, 0.1f, 0.85f, 0.1f, 0.1f],
            FrameRate = 10.0,
            ValidFrameCount = 7
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.10,
            MinDownbeatSpacingSeconds = 0.10,
            MaxDownbeatSnapDistanceSeconds = 0.11
        });

        var result = postprocessor.Postprocess(inference, availability);

        result.BeatTimes.Should().Equal(0.0, 0.2, 0.4, 0.6);
        result.DownbeatTimes.Should().Equal(0.0, 0.4);
    }

    [Fact]
    public void Postprocess_estimates_meter_from_snapped_downbeat_gaps()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations =
            [
                0.9f, 0.1f,
                0.9f, 0.1f,
                0.9f, 0.1f,
                0.9f, 0.1f,
                0.9f, 0.1f,
                0.9f, 0.1f,
                0.9f, 0.1f,
                0.9f
            ],
            DownbeatActivations =
            [
                0.9f, 0.1f,
                0.1f, 0.1f,
                0.1f, 0.1f,
                0.1f, 0.1f,
                0.9f, 0.1f,
                0.1f, 0.1f,
                0.1f, 0.1f,
                0.1f
            ],
            FrameRate = 2.0,
            ValidFrameCount = 15
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.25,
            MinDownbeatSpacingSeconds = 0.25,
            MaxDownbeatSnapDistanceSeconds = 0.01
        });

        var result = postprocessor.Postprocess(inference, availability);

        result.EstimatedMeter.Should().Be(4);
        result.DownbeatTimes.Should().Equal(0.0, 4.0);
        result.BeatNumbers.Should().StartWith([1, 2, 3, 4, 1]);
    }

    [Fact]
    public void Postprocess_fills_beat_numbers_without_downbeats()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = [0.9f, 0.1f, 0.9f, 0.1f, 0.9f],
            DownbeatActivations = [0.1f, 0.1f, 0.1f, 0.1f, 0.1f],
            FrameRate = 2.0,
            ValidFrameCount = 5
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.25,
            DefaultMeter = 4
        });

        var result = postprocessor.Postprocess(inference, availability);

        result.DownbeatTimes.Should().BeEmpty();
        result.EstimatedMeter.Should().Be(4);
        result.BeatNumbers.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Postprocess_ignores_unsnapped_downbeats()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = [0.9f, 0.1f, 0.9f, 0.1f, 0.9f],
            DownbeatActivations = [0.1f, 0.9f, 0.1f, 0.1f, 0.1f],
            FrameRate = 10.0,
            ValidFrameCount = 5
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.10,
            MinDownbeatSpacingSeconds = 0.10,
            MaxDownbeatSnapDistanceSeconds = 0.01
        });

        var result = postprocessor.Postprocess(inference, availability);

        result.DownbeatTimes.Should().BeEmpty();
    }

    [Fact]
    public void Postprocess_rejects_empty_beat_result()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = [0.1f, 0.2f, 0.1f],
            DownbeatActivations = [0.1f, 0.2f, 0.1f],
            FrameRate = 2.0,
            ValidFrameCount = 3
        };
        var postprocessor = new BeatThisPostprocessor();

        var act = () => postprocessor.Postprocess(inference, availability);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Beat This postprocessor did not detect any beats.");
    }

    [Fact]
    public void Postprocess_does_not_create_artificial_peaks_when_activations_are_flat()
    {
        var availability = CreateAvailability();
        var inference = new BeatThisInferenceResult
        {
            BeatActivations = Enumerable.Repeat(0.1f, 100).ToArray(),
            DownbeatActivations = Enumerable.Repeat(0.1f, 100).ToArray(),
            FrameRate = 100.0,
            ValidFrameCount = 100,
            AudioDurationSeconds = 1.0
        };
        var postprocessor = new BeatThisPostprocessor(new BeatThisPostprocessorOptions
        {
            BeatThreshold = 0.5,
            DownbeatThreshold = 0.5,
            MinBeatSpacingSeconds = 0.30
        });

        var act = () => postprocessor.Postprocess(inference, availability);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Beat This postprocessor did not detect any beats.");
    }

    private static BeatThisAvailability CreateAvailability()
    {
        return BeatThisAvailability.Available(
            "C:\\Models\\beat-this-large.onnx",
            "C:\\Models\\model.json",
            "abc123",
            new BeatThisModelMetadata
            {
                Name = "beat-this-large",
                Version = "test-version",
                License = "MIT",
                ModelSha256 = "abc123"
            });
    }
}