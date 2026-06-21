using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;
using EternalLoop.AnalysisEngine.Core.BeatTracking.BeatThis;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public sealed class BeatThisOnnxBeatTracker : IBeatTracker
{
    private readonly BeatThisModelLocator _modelLocator;
    private readonly Func<string, IBeatModelRuntime> _runtimeFactory;
    private readonly BeatThisOfficialAggregateRunner _aggregateRunner;
    private readonly BeatThisAdvisorPostprocessor _advisorPostprocessor;
    private readonly BeatGridGuardrailOptions _guardrailOptions;
    private readonly BeatThisDownbeatSanitizer _downbeatSanitizer;

    public BeatThisOnnxBeatTracker(
        BeatThisModelLocator? modelLocator = null,
        Func<string, IBeatModelRuntime>? runtimeFactory = null,
        BeatThisPostprocessor? postprocessor = null,
        BeatThisOfficialAggregateRunner? aggregateRunner = null,
        BeatThisAdvisorPostprocessor? advisorPostprocessor = null,
        BeatGridGuardrailOptions? guardrailOptions = null)
    {
        _modelLocator = modelLocator ?? new BeatThisModelLocator();
        _runtimeFactory = runtimeFactory ?? (modelPath => new OnnxBeatModelRuntime(modelPath));
        _aggregateRunner = aggregateRunner ?? new BeatThisOfficialAggregateRunner();
        _advisorPostprocessor = advisorPostprocessor ?? new BeatThisAdvisorPostprocessor();
        _guardrailOptions = guardrailOptions ?? new BeatGridGuardrailOptions();
        _downbeatSanitizer = new BeatThisDownbeatSanitizer();
    }

    public BeatTrackingResult Track(
        LoadedAudio audio,
        FeatureMatrix features,
        BeatTrackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(options);

        var availability = _modelLocator.GetAvailability();

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException($"Beat This model is unavailable: {availability.ErrorMessage}");
        }

        var metadata = availability.Metadata ?? new BeatThisModelMetadata();
        var preprocessor = new BeatThisPreprocessor(BeatThisPreprocessorOptions.FromMetadata(metadata));
        var spectrogram = preprocessor.PreprocessFullTrack(audio);

        using var runtime = _runtimeFactory(availability.ModelPath!);
        var advisorOutput = _aggregateRunner.Run(spectrogram, runtime, metadata);
        var advisorResult = _advisorPostprocessor.Postprocess(advisorOutput);

        if (advisorResult.IsDenseGrid)
        {
            throw new InvalidDataException(
                $"Beat This advisor postprocess rejected dense grid: {advisorResult.RejectionReason}");
        }

        var sanitizationResult = _downbeatSanitizer.Sanitize(
            advisorResult.BeatTimes,
            advisorResult.DownbeatTimes,
            _guardrailOptions.MaxDownbeatToBeatDistanceSeconds);

        var coverageSeconds = advisorOutput.FrameCount / advisorOutput.FrameRate;
        var coverageRatio = advisorOutput.DurationSeconds <= 0.0
            ? 0.0
            : Math.Min(1.0, coverageSeconds / advisorOutput.DurationSeconds);

        return new BeatTrackingResult
        {
            EstimatedBpm = advisorResult.EstimatedBpm,
            BeatTimes = advisorResult.BeatTimes,
            Confidences = advisorResult.BeatConfidences,
            DownbeatTimes = sanitizationResult.Downbeats.ToArray(),
            ProviderWarnings = sanitizationResult.Warnings,
            DownbeatSanitized = sanitizationResult.Sanitized,
            ProviderName = "beat-this",
            ProviderVersion = metadata.Version,
            ProviderLicense = metadata.License,
            ModelName = metadata.Name,
            ModelSha256 = availability.ModelSha256 ?? metadata.ModelSha256 ?? "none",
            UsedAiProvider = true,
            UsedBuiltInProvider = false,
            UsedFallbackProvider = false,
            BeatGridMode = "beat-this-advisor",
            BeatProviderOutputMode = advisorOutput.OutputMode,
            BeatProviderChunkCount = advisorOutput.ChunkCount,
            BeatProviderValidFrameCount = advisorOutput.FrameCount,
            BeatProviderCoverageSeconds = coverageSeconds,
            BeatProviderCoverageRatio = coverageRatio,
            BeatActivationSummary = BeatThisActivationSummary.From(
                advisorOutput.BeatLogits,
                advisorOutput.FrameCount,
                0.0),
            DownbeatActivationSummary = BeatThisActivationSummary.From(
                advisorOutput.DownbeatLogits,
                advisorOutput.FrameCount,
                0.0)
        };
    }
}
