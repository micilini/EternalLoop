using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace EternalLoop.Core.AI;

public sealed class OnnxMusicEmbeddingModel : ILocalMusicEmbeddingModel
{
    private const int InputRank = 3;
    private const int OutputRank = 2;
    private const int InputBatchDimensionIndex = 0;
    private const int InputMelBandsDimensionIndex = 1;
    private const int InputPatchFramesDimensionIndex = 2;
    private const int OutputBatchDimensionIndex = 0;
    private const int OutputEmbeddingDimensionIndex = 1;

    private readonly ILogger<OnnxMusicEmbeddingModel> _logger;
    private readonly AiModelManifest _manifest;
    private readonly string _onnxPath;
    private readonly InferenceSession _session;
    private bool _disposed;

    public OnnxMusicEmbeddingModel(
        AiModelManifestLoader manifestLoader,
        AiModelPathResolver pathResolver,
        ILogger<OnnxMusicEmbeddingModel> logger)
    {
        ArgumentNullException.ThrowIfNull(manifestLoader);
        ArgumentNullException.ThrowIfNull(pathResolver);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _manifest = manifestLoader.Load();
        _onnxPath = pathResolver.ResolveOnnxPath(_manifest);
        _session = CreateSession();
        ValidateModelShape();
    }

    public string ModelId => _manifest.Id;

    public int BatchSize => _manifest.BatchSize;

    public int MelBands => _manifest.MelBands;

    public int PatchFrames => _manifest.PatchFrames;

    public int EmbeddingDimensions => _manifest.EmbeddingDimensions;

    public IReadOnlyList<float[]> Predict(IReadOnlyList<float[][]> patches)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (patches is null)
        {
            throw new ArgumentNullException(nameof(patches));
        }

        if (patches.Count == 0)
        {
            throw new ArgumentException("At least one AI input patch is required.", nameof(patches));
        }

        if (patches.Count > BatchSize)
        {
            throw new ArgumentException($"AI input patch count cannot exceed batch size '{BatchSize}'.", nameof(patches));
        }

        ValidatePatches(patches);

        try
        {
            var tensor = CreateInputTensor(patches);
            var input = NamedOnnxValue.CreateFromTensor(_manifest.InputName, tensor);
            using var results = _session.Run([input], [_manifest.EmbeddingOutputName]);
            var output = results.FirstOrDefault(result => string.Equals(result.Name, _manifest.EmbeddingOutputName, StringComparison.Ordinal));

            if (output is null)
            {
                throw new OnnxInferenceException($"ONNX model '{ModelId}' did not return expected output '{_manifest.EmbeddingOutputName}'.");
            }

            var outputTensor = output.AsTensor<float>();
            ValidateOutputTensor(outputTensor);
            return ReadRealEmbeddings(outputTensor, patches.Count);
        }
        catch (OnnxInferenceException)
        {
            throw;
        }
        catch (OnnxRuntimeException ex)
        {
            throw new OnnxInferenceException($"ONNX inference failed for model '{ModelId}' at '{_onnxPath}'.", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _disposed = true;
    }

    private InferenceSession CreateSession()
    {
        try
        {
            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            return new InferenceSession(_onnxPath, sessionOptions);
        }
        catch (OnnxRuntimeException ex)
        {
            throw new OnnxInferenceException($"ONNX session failed to load model '{ModelId}' from '{_onnxPath}'.", ex);
        }
    }

    private void ValidateModelShape()
    {
        if (!_session.InputMetadata.TryGetValue(_manifest.InputName, out var inputMetadata) || inputMetadata is null)
        {
            throw new OnnxInferenceException($"ONNX model '{ModelId}' is missing expected input '{_manifest.InputName}' at '{_onnxPath}'. Available inputs: {FormatMetadata(_session.InputMetadata)}.");
        }

        if (!_session.OutputMetadata.TryGetValue(_manifest.EmbeddingOutputName, out var outputMetadata) || outputMetadata is null)
        {
            throw new OnnxInferenceException($"ONNX model '{ModelId}' is missing expected output '{_manifest.EmbeddingOutputName}' at '{_onnxPath}'. Available outputs: {FormatMetadata(_session.OutputMetadata)}.");
        }

        ValidateDimensions(inputMetadata.Dimensions, [_manifest.BatchSize, _manifest.MelBands, _manifest.PatchFrames], InputRank, "input", _manifest.InputName);
        ValidateDimensions(outputMetadata.Dimensions, [_manifest.BatchSize, _manifest.EmbeddingDimensions], OutputRank, "output", _manifest.EmbeddingOutputName);
        _logger.LogInformation("Loaded ONNX AI model {ModelId} from {ModelPath}", ModelId, _onnxPath);
    }

    private static string FormatMetadata(IReadOnlyDictionary<string, NodeMetadata> metadata)
    {
        return string.Join(", ", metadata.Select(pair => $"{pair.Key}[{string.Join("x", pair.Value.Dimensions)}]"));
    }

    private void ValidateDimensions(IReadOnlyList<int> actual, IReadOnlyList<int> expected, int expectedRank, string kind, string name)
    {
        if (actual.Count != expectedRank)
        {
            throw new OnnxInferenceException($"ONNX model '{ModelId}' {kind} '{name}' must have rank '{expectedRank}' but has rank '{actual.Count}'.");
        }

        for (var index = 0; index < expected.Count; index++)
        {
            if (actual[index] > 0 && actual[index] != expected[index])
            {
                throw new OnnxInferenceException($"ONNX model '{ModelId}' {kind} '{name}' dimension '{index}' must be '{expected[index]}' but was '{actual[index]}'.");
            }
        }
    }

    private void ValidatePatches(IReadOnlyList<float[][]> patches)
    {
        for (var patchIndex = 0; patchIndex < patches.Count; patchIndex++)
        {
            var patch = patches[patchIndex];
            if (patch is null || patch.Length != MelBands)
            {
                throw new ArgumentException($"AI input patch '{patchIndex}' must have '{MelBands}' mel bands.", nameof(patches));
            }

            for (var melBandIndex = 0; melBandIndex < patch.Length; melBandIndex++)
            {
                if (patch[melBandIndex] is null || patch[melBandIndex].Length != PatchFrames)
                {
                    throw new ArgumentException($"AI input patch '{patchIndex}' mel band '{melBandIndex}' must have '{PatchFrames}' frames.", nameof(patches));
                }
            }
        }
    }

    private DenseTensor<float> CreateInputTensor(IReadOnlyList<float[][]> patches)
    {
        var tensor = new DenseTensor<float>([BatchSize, MelBands, PatchFrames]);

        for (var batchIndex = 0; batchIndex < BatchSize; batchIndex++)
        {
            var sourcePatch = patches[Math.Min(batchIndex, patches.Count - 1)];
            for (var melBandIndex = 0; melBandIndex < MelBands; melBandIndex++)
            {
                for (var frameIndex = 0; frameIndex < PatchFrames; frameIndex++)
                {
                    tensor[batchIndex, melBandIndex, frameIndex] = sourcePatch[melBandIndex][frameIndex];
                }
            }
        }

        return tensor;
    }

    private void ValidateOutputTensor(Tensor<float> outputTensor)
    {
        if (outputTensor.Dimensions.Length != OutputRank)
        {
            throw new OnnxInferenceException($"ONNX model '{ModelId}' output '{_manifest.EmbeddingOutputName}' must have rank '{OutputRank}' but has rank '{outputTensor.Dimensions.Length}'.");
        }

        if (outputTensor.Dimensions[OutputBatchDimensionIndex] < BatchSize)
        {
            throw new OnnxInferenceException($"ONNX model '{ModelId}' output batch must be at least '{BatchSize}' but was '{outputTensor.Dimensions[OutputBatchDimensionIndex]}'.");
        }

        if (outputTensor.Dimensions[OutputEmbeddingDimensionIndex] != EmbeddingDimensions)
        {
            throw new OnnxInferenceException($"ONNX model '{ModelId}' embedding dimension must be '{EmbeddingDimensions}' but was '{outputTensor.Dimensions[OutputEmbeddingDimensionIndex]}'.");
        }
    }

    private IReadOnlyList<float[]> ReadRealEmbeddings(Tensor<float> outputTensor, int realPatchCount)
    {
        var embeddings = new List<float[]>(realPatchCount);

        for (var patchIndex = 0; patchIndex < realPatchCount; patchIndex++)
        {
            var embedding = new float[EmbeddingDimensions];
            for (var dimensionIndex = 0; dimensionIndex < EmbeddingDimensions; dimensionIndex++)
            {
                embedding[dimensionIndex] = outputTensor[patchIndex, dimensionIndex];
            }

            embeddings.Add(embedding);
        }

        return embeddings;
    }
}
