namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public static class BeatThisOutputMapper
{
    public static BeatThisInferenceResult Map(
        IReadOnlyList<BeatThisOutputTensor> outputs,
        BeatThisInputTensor inputTensor,
        BeatThisModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(inputTensor);
        ArgumentNullException.ThrowIfNull(metadata);

        if (outputs.Count == 0)
        {
            throw new InvalidDataException("Beat This ONNX model did not return any outputs.");
        }

        var validFrameCount = inputTensor.ValidFrameCount;

        if (TryMapCombinedOutput(outputs, validFrameCount, out var combinedResult))
        {
            return new BeatThisInferenceResult
            {
                BeatActivations = combinedResult.BeatActivations,
                DownbeatActivations = combinedResult.DownbeatActivations,
                FrameRate = inputTensor.FrameRate,
                ValidFrameCount = validFrameCount,
                StartFrameIndex = inputTensor.StartFrameIndex,
                StartTimeSeconds = inputTensor.StartTimeSeconds,
                AudioDurationSeconds = inputTensor.DurationSeconds,
                OutputNames = outputs.Select(output => output.Name).ToArray(),
                OutputMode = combinedResult.OutputMode
            };
        }

        var beatOutput = ResolveBeatOutput(outputs, metadata);
        var downbeatOutput = ResolveDownbeatOutput(outputs, metadata);

        if (beatOutput is null)
        {
            throw new InvalidDataException("Could not resolve Beat This beat output tensor.");
        }

        if (downbeatOutput is null)
        {
            throw new InvalidDataException("Could not resolve Beat This downbeat output tensor.");
        }

        return new BeatThisInferenceResult
        {
            BeatActivations = CopyFrameVector(beatOutput, validFrameCount),
            DownbeatActivations = CopyFrameVector(downbeatOutput, validFrameCount),
            FrameRate = inputTensor.FrameRate,
            ValidFrameCount = validFrameCount,
            StartFrameIndex = inputTensor.StartFrameIndex,
            StartTimeSeconds = inputTensor.StartTimeSeconds,
            AudioDurationSeconds = inputTensor.DurationSeconds,
            OutputNames = outputs.Select(output => output.Name).ToArray(),
            OutputMode = "separate-beat-downbeat-outputs"
        };
    }

    private static bool TryMapCombinedOutput(
        IReadOnlyList<BeatThisOutputTensor> outputs,
        int validFrameCount,
        out BeatThisInferenceResult result)
    {
        if (outputs.Count != 1)
        {
            result = null!;
            return false;
        }

        foreach (var output in outputs)
        {
            if (output.Dimensions.Length < 2)
            {
                continue;
            }

            if (output.Dimensions.Length >= 3
                && output.Dimensions[^2] is >= 2 and <= 8
                && output.Dimensions[^1] >= validFrameCount)
            {
                result = MapChannelFirstCombinedOutput(output, validFrameCount);

                return true;
            }

            var channels = output.Dimensions[^1];
            var frameCount = output.Dimensions[^2];

            if (channels < 2 || frameCount <= 0)
            {
                continue;
            }

            var framesToCopy = Math.Min(validFrameCount, frameCount);
            var beatActivations = new float[validFrameCount];
            var downbeatActivations = new float[validFrameCount];

            for (var frame = 0; frame < framesToCopy; frame++)
            {
                var baseOffset = frame * channels;

                if (baseOffset + 1 >= output.Data.Length)
                {
                    break;
                }

                beatActivations[frame] = output.Data[baseOffset];
                downbeatActivations[frame] = output.Data[baseOffset + 1];
            }

            result = new BeatThisInferenceResult
            {
                BeatActivations = beatActivations,
                DownbeatActivations = downbeatActivations,
                FrameRate = 0.0,
                ValidFrameCount = validFrameCount,
                OutputNames = [],
                OutputMode = "combined-frame-channel-output"
            };

            return true;
        }

        result = null!;
        return false;
    }

    private static BeatThisInferenceResult MapChannelFirstCombinedOutput(
        BeatThisOutputTensor output,
        int validFrameCount)
    {
        var channels = output.Dimensions[^2];
        var frameCount = output.Dimensions[^1];
        var framesToCopy = Math.Min(validFrameCount, frameCount);
        var beatActivations = new float[validFrameCount];
        var downbeatActivations = new float[validFrameCount];

        for (var frame = 0; frame < framesToCopy; frame++)
        {
            var beatOffset = frame;
            var downbeatOffset = frameCount + frame;

            if (channels < 2 || downbeatOffset >= output.Data.Length)
            {
                break;
            }

            beatActivations[frame] = output.Data[beatOffset];
            downbeatActivations[frame] = output.Data[downbeatOffset];
        }

        return new BeatThisInferenceResult
        {
            BeatActivations = beatActivations,
            DownbeatActivations = downbeatActivations,
            FrameRate = 0.0,
            ValidFrameCount = validFrameCount,
            OutputNames = [],
            OutputMode = "combined-channel-frame-output"
        };
    }

    private static BeatThisOutputTensor? ResolveBeatOutput(
        IReadOnlyList<BeatThisOutputTensor> outputs,
        BeatThisModelMetadata metadata)
    {
        if (metadata.OutputNames.Length >= 1)
        {
            var configured = FindByName(outputs, metadata.OutputNames[0]);

            if (configured is not null)
            {
                return configured;
            }
        }

        return outputs.FirstOrDefault(output =>
            output.Name.Contains("beat", StringComparison.OrdinalIgnoreCase)
            && !output.Name.Contains("down", StringComparison.OrdinalIgnoreCase));
    }

    private static BeatThisOutputTensor? ResolveDownbeatOutput(
        IReadOnlyList<BeatThisOutputTensor> outputs,
        BeatThisModelMetadata metadata)
    {
        if (metadata.OutputNames.Length >= 2)
        {
            var configured = FindByName(outputs, metadata.OutputNames[1]);

            if (configured is not null)
            {
                return configured;
            }
        }

        return outputs.FirstOrDefault(output =>
            output.Name.Contains("down", StringComparison.OrdinalIgnoreCase));
    }

    private static BeatThisOutputTensor? FindByName(
        IReadOnlyList<BeatThisOutputTensor> outputs,
        string name)
    {
        return outputs.FirstOrDefault(output =>
            string.Equals(output.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static float[] CopyFrameVector(BeatThisOutputTensor output, int validFrameCount)
    {
        var activations = new float[validFrameCount];
        var framesToCopy = Math.Min(validFrameCount, output.Data.Length);

        Array.Copy(output.Data, activations, framesToCopy);

        return activations;
    }
}
