using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.AI;

public sealed class AiEmbeddingExtractor : IAiEmbeddingExtractor
{
    private const double StartProgress = 0.0;
    private const double AudioPreparedProgress = 0.2;
    private const double MelSpectrogramProgress = 0.45;
    private const double PatchExtractionProgress = 0.65;
    private const double EmbeddingExtractionProgress = 0.9;
    private const double CompleteProgress = 1.0;
    private const int MinimumRealPatchCount = 1;

    private readonly AiAudioPreprocessor _audioPreprocessor;
    private readonly AiMelSpectrogramExtractor _melSpectrogramExtractor;
    private readonly AiPatchExtractor _patchExtractor;
    private readonly AiPatchBatcher _patchBatcher;
    private readonly ILocalMusicEmbeddingModel _embeddingModel;
    private readonly IAiModelProvider _modelProvider;

    public AiEmbeddingExtractor(
        AiAudioPreprocessor audioPreprocessor,
        AiMelSpectrogramExtractor melSpectrogramExtractor,
        AiPatchExtractor patchExtractor,
        AiPatchBatcher patchBatcher,
        ILocalMusicEmbeddingModel embeddingModel,
        IAiModelProvider modelProvider)
    {
        _audioPreprocessor = audioPreprocessor ?? throw new ArgumentNullException(nameof(audioPreprocessor));
        _melSpectrogramExtractor = melSpectrogramExtractor ?? throw new ArgumentNullException(nameof(melSpectrogramExtractor));
        _patchExtractor = patchExtractor ?? throw new ArgumentNullException(nameof(patchExtractor));
        _patchBatcher = patchBatcher ?? throw new ArgumentNullException(nameof(patchBatcher));
        _embeddingModel = embeddingModel ?? throw new ArgumentNullException(nameof(embeddingModel));
        _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
    }

    public async Task<AiEmbeddingExtractionResult> ExtractAsync(
        LoadedAudio audio,
        IAnalysisProgressReporter progressReporter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(progressReporter);

        if (audio.Samples is null)
        {
            throw new ArgumentException("Audio samples are required.", nameof(audio));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var manifest = await _modelProvider.GetManifestAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (audio.Samples.Length == 0)
        {
            return CreateResult(manifest, []);
        }

        progressReporter.Report(AnalysisStage.RunningAi, StartProgress, "Preparing local AI audio");
        var samples = _audioPreprocessor.ResampleToModelRate(audio, AiPreprocessingDefaultValues.SampleRate);
        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.RunningAi, AudioPreparedProgress, "Building AI mel spectrogram");
        var melSpectrogram = _melSpectrogramExtractor.Extract(
            samples,
            AiPreprocessingDefaultValues.SampleRate,
            AiPreprocessingDefaultValues.MelBands,
            AiPreprocessingDefaultValues.FftSize,
            AiPreprocessingDefaultValues.HopLength);
        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.RunningAi, MelSpectrogramProgress, "Creating AI patches");
        var patches = _patchExtractor.ExtractPatches(
            melSpectrogram,
            _embeddingModel.MelBands,
            _embeddingModel.PatchFrames,
            AiPreprocessingDefaultValues.PatchHopFrames);
        var batches = _patchBatcher.CreateBatches(patches, _embeddingModel.BatchSize);
        cancellationToken.ThrowIfCancellationRequested();

        if (patches.Count == 0 || batches.Count == 0)
        {
            return CreateResult(manifest, []);
        }

        progressReporter.Report(AnalysisStage.RunningAi, PatchExtractionProgress, "Extracting AI embeddings");
        var frames = ExtractFrames(batches, patches.Count, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        progressReporter.Report(AnalysisStage.RunningAi, EmbeddingExtractionProgress, "AI embeddings ready");
        progressReporter.Report(AnalysisStage.RunningAi, CompleteProgress, "AI embeddings ready");
        return CreateResult(manifest, frames);
    }

    private IReadOnlyList<AiEmbeddingFrame> ExtractFrames(
        IReadOnlyList<AiPatchBatch> batches,
        int totalPatchCount,
        CancellationToken cancellationToken)
    {
        var frames = new List<AiEmbeddingFrame>();
        var patchIndex = 0;

        for (var batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = batches[batchIndex];
            ValidateBatch(batch, batchIndex, totalPatchCount);
            var realPatches = batch.Patches.Take(batch.RealPatchCount).ToArray();
            var embeddings = _embeddingModel.Predict(realPatches);

            if (embeddings.Count != batch.RealPatchCount)
            {
                throw new InvalidOperationException($"AI model returned '{embeddings.Count}' embeddings for '{batch.RealPatchCount}' real patches in batch '{batchIndex}' with batch size '{_embeddingModel.BatchSize}' and total patch count '{totalPatchCount}'.");
            }

            for (var embeddingIndex = 0; embeddingIndex < embeddings.Count; embeddingIndex++)
            {
                var normalized = Normalize(embeddings[embeddingIndex]);
                frames.Add(new AiEmbeddingFrame
                {
                    Index = patchIndex,
                    Start = CalculatePatchStart(patchIndex),
                    Duration = CalculatePatchDuration(),
                    Vector = normalized
                });
                patchIndex++;
            }
        }

        return frames;
    }

    private void ValidateBatch(AiPatchBatch batch, int batchIndex, int totalPatchCount)
    {
        if (batch.Patches is null)
        {
            throw new InvalidOperationException($"AI patch batch '{batchIndex}' has no patch collection for total patch count '{totalPatchCount}'.");
        }

        if (batch.RealPatchCount < MinimumRealPatchCount
            || batch.RealPatchCount > _embeddingModel.BatchSize
            || batch.RealPatchCount > batch.Patches.Count)
        {
            throw new InvalidOperationException($"AI patch batch '{batchIndex}' has invalid real patch count '{batch.RealPatchCount}' with batch size '{_embeddingModel.BatchSize}', batch patch count '{batch.Patches.Count}' and total patch count '{totalPatchCount}'.");
        }
    }

    private static AiEmbeddingExtractionResult CreateResult(AiModelManifest manifest, IReadOnlyList<AiEmbeddingFrame> frames)
    {
        return new AiEmbeddingExtractionResult
        {
            ModelId = manifest.Id,
            ModelVersion = manifest.Version,
            SampleRate = AiPreprocessingDefaultValues.SampleRate,
            EmbeddingDimensions = AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions,
            Frames = frames
        };
    }

    private static float[] Normalize(float[] vector)
    {
        ArgumentNullException.ThrowIfNull(vector);

        if (vector.Length != AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions)
        {
            throw new InvalidOperationException($"AI embedding vector must have '{AiModelDefaultValues.DiscogsEffNetEmbeddingDimensions}' dimensions but had '{vector.Length}'.");
        }

        var sanitized = vector.Select(Sanitize).ToArray();
        var sumSquares = sanitized.Sum(value => (double)value * value);
        var norm = Math.Sqrt(sumSquares);

        if (norm <= AiPreprocessingDefaultValues.NormalizationEpsilon)
        {
            return sanitized;
        }

        return sanitized.Select(value => Sanitize((float)(value / norm))).ToArray();
    }

    private static double CalculatePatchStart(int patchIndex)
    {
        return patchIndex
            * AiPreprocessingDefaultValues.PatchHopFrames
            * AiPreprocessingDefaultValues.HopLength
            / (double)AiPreprocessingDefaultValues.SampleRate;
    }

    private static double CalculatePatchDuration()
    {
        return AiPreprocessingDefaultValues.PatchFrames
            * AiPreprocessingDefaultValues.HopLength
            / (double)AiPreprocessingDefaultValues.SampleRate;
    }

    private static float Sanitize(float value)
    {
        return float.IsFinite(value) ? value : 0.0f;
    }
}
