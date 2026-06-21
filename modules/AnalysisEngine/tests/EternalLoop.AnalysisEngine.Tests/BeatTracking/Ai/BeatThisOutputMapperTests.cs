using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai;

public sealed class BeatThisOutputMapperTests
{
    [Fact]
    public void Map_maps_separate_beat_and_downbeat_outputs_by_metadata_names()
    {
        var input = CreateInputTensor(validFrameCount: 4);
        var metadata = new BeatThisModelMetadata
        {
            OutputNames = ["beat_logits", "downbeat_logits"]
        };
        var outputs = new[]
        {
            new BeatThisOutputTensor
            {
                Name = "beat_logits",
                Data = [0.1f, 0.8f, 0.2f, 0.9f],
                Dimensions = [4]
            },
            new BeatThisOutputTensor
            {
                Name = "downbeat_logits",
                Data = [0.7f, 0.1f, 0.6f, 0.1f],
                Dimensions = [4]
            }
        };

        var result = BeatThisOutputMapper.Map(outputs, input, metadata);

        result.BeatActivations.Should().Equal(0.1f, 0.8f, 0.2f, 0.9f);
        result.DownbeatActivations.Should().Equal(0.7f, 0.1f, 0.6f, 0.1f);
        result.OutputMode.Should().Be("separate-beat-downbeat-outputs");
    }

    [Fact]
    public void Map_maps_combined_frame_channel_output()
    {
        var input = CreateInputTensor(validFrameCount: 3);
        var metadata = new BeatThisModelMetadata();
        var outputs = new[]
        {
            new BeatThisOutputTensor
            {
                Name = "logits",
                Data =
                [
                    0.1f, 0.9f,
                    0.8f, 0.2f,
                    0.3f, 0.7f
                ],
                Dimensions = [1, 3, 2]
            }
        };

        var result = BeatThisOutputMapper.Map(outputs, input, metadata);

        result.BeatActivations.Should().Equal(0.1f, 0.8f, 0.3f);
        result.DownbeatActivations.Should().Equal(0.9f, 0.2f, 0.7f);
        result.OutputMode.Should().Be("combined-frame-channel-output");
    }

    [Fact]
    public void Map_rejects_empty_outputs()
    {
        var input = CreateInputTensor(validFrameCount: 4);

        var act = () => BeatThisOutputMapper.Map([], input, new BeatThisModelMetadata());

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Beat This ONNX model did not return any outputs.");
    }

    [Fact]
    public void Map_rejects_unresolvable_outputs()
    {
        var input = CreateInputTensor(validFrameCount: 4);
        var outputs = new[]
        {
            new BeatThisOutputTensor
            {
                Name = "unknown",
                Data = [0.1f, 0.2f, 0.3f, 0.4f],
                Dimensions = [4]
            }
        };

        var act = () => BeatThisOutputMapper.Map(outputs, input, new BeatThisModelMetadata());

        act.Should().Throw<InvalidDataException>()
            .WithMessage("Could not resolve Beat This beat output tensor.");
    }

    [Fact]
    public void Map_preserves_chunk_timing_metadata()
    {
        var input = new BeatThisInputTensor(
            [0.0f, 0.0f, 0.0f, 0.0f],
            [1, 2, 2],
            validFrameCount: 2,
            sampleRate: 22_050,
            frameRate: 100.0,
            chunkFrames: 2,
            melBins: 2,
            frameSize: 1_024,
            hopSize: 220,
            durationSeconds: 30.0,
            startFrameIndex: 1_500,
            startTimeSeconds: 15.0);
        var metadata = new BeatThisModelMetadata
        {
            OutputNames = ["beat_logits", "downbeat_logits"]
        };
        var outputs = new[]
        {
            new BeatThisOutputTensor
            {
                Name = "beat_logits",
                Data = [0.1f, 0.8f],
                Dimensions = [2]
            },
            new BeatThisOutputTensor
            {
                Name = "downbeat_logits",
                Data = [0.7f, 0.1f],
                Dimensions = [2]
            }
        };

        var result = BeatThisOutputMapper.Map(outputs, input, metadata);

        result.StartFrameIndex.Should().Be(1_500);
        result.StartTimeSeconds.Should().Be(15.0);
        result.AudioDurationSeconds.Should().Be(30.0);
    }

    private static BeatThisInputTensor CreateInputTensor(int validFrameCount)
    {
        return new BeatThisInputTensor(
            new float[8 * 4],
            [1, 8, 4],
            validFrameCount,
            22_050,
            100.0,
            8,
            4,
            1_024,
            220,
            1.0);
    }
}