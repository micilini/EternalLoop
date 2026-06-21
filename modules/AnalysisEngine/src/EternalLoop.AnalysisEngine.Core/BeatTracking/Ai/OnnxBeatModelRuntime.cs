using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class OnnxBeatModelRuntime : IBeatModelRuntime
{
    private readonly InferenceSession _session;

    public OnnxBeatModelRuntime(string modelPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        var fullPath = Path.GetFullPath(modelPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"ONNX beat model file not found: {fullPath}", fullPath);
        }

        ModelPath = fullPath;
        _session = new InferenceSession(ModelPath);
        InputNames = _session.InputMetadata.Keys.ToArray();
        OutputNames = _session.OutputMetadata.Keys.ToArray();
    }

    public string ModelPath { get; }

    public IReadOnlyList<string> InputNames { get; }

    public IReadOnlyList<string> OutputNames { get; }

    public BeatThisInferenceResult Run(
        BeatThisInputTensor inputTensor,
        BeatThisModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(inputTensor);
        ArgumentNullException.ThrowIfNull(metadata);

        var inputName = ResolveInputName(metadata);
        var tensorShape = inputTensor.Shape.Select(dimension => checked((int)dimension)).ToArray();
        var tensor = new DenseTensor<float>(inputTensor.Data, tensorShape);

        var input = NamedOnnxValue.CreateFromTensor(inputName, tensor);

        using var results = _session.Run(new[] { input });

        var outputTensors = results
            .Select(ToBeatThisOutputTensor)
            .ToArray();

        return BeatThisOutputMapper.Map(outputTensors, inputTensor, metadata);
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private string ResolveInputName(BeatThisModelMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.InputName)
            && InputNames.Contains(metadata.InputName, StringComparer.OrdinalIgnoreCase))
        {
            return InputNames.First(name =>
                string.Equals(name, metadata.InputName, StringComparison.OrdinalIgnoreCase));
        }

        if (InputNames.Count == 1)
        {
            return InputNames[0];
        }

        throw new InvalidDataException(
            $"Could not resolve Beat This ONNX input name. Metadata requested '{metadata.InputName}', model inputs: {string.Join(", ", InputNames)}.");
    }

    private static BeatThisOutputTensor ToBeatThisOutputTensor(DisposableNamedOnnxValue output)
    {
        var tensor = output.AsTensor<float>();
        var data = new float[tensor.Length];
        var index = 0;

        foreach (var value in tensor)
        {
            data[index++] = value;
        }

        return new BeatThisOutputTensor
        {
            Name = output.Name,
            Data = data,
            Dimensions = tensor.Dimensions.ToArray()
        };
    }
}
