using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai.Advisor;
using EternalLoop.AnalysisEngine.Core.Export;
using EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;
using EternalLoop.AnalysisEngine.Core.Export.Summary;
using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.AnalysisEngine.Cli.Composition;

public static class AnalysisEngineServiceRegistration
{
    public static IServiceCollection AddAnalysisEngineServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSingleton<IAudioLoader, NAudioAnalysisAudioLoader>();
        services.AddSingleton<IFeatureExtractor, NWavesAnalysisFeatureExtractor>();

        services.AddSingleton<SpectralFluxBeatTracker>();
        services.AddSingleton<BeatGridGuardrails>();
        services.AddSingleton<BeatThisModelLocator>();
        services.AddSingleton<BeatThisPostprocessor>();
        services.AddSingleton<BeatThisOfficialAggregateRunner>();
        services.AddSingleton<BeatThisAdvisorPostprocessor>();

        services.AddSingleton<Func<string, IBeatModelRuntime>>(_ =>
            modelPath => new OnnxBeatModelRuntime(modelPath));

        services.AddSingleton<BeatThisOnnxBeatTracker>(provider =>
            new BeatThisOnnxBeatTracker(
                provider.GetRequiredService<BeatThisModelLocator>(),
                provider.GetRequiredService<Func<string, IBeatModelRuntime>>(),
                provider.GetRequiredService<BeatThisPostprocessor>(),
                provider.GetRequiredService<BeatThisOfficialAggregateRunner>(),
                provider.GetRequiredService<BeatThisAdvisorPostprocessor>()));

        services.AddSingleton<IBeatTracker>(provider =>
            new BeatTrackerSelector(
                provider.GetRequiredService<SpectralFluxBeatTracker>(),
                provider.GetRequiredService<BeatThisOnnxBeatTracker>(),
                provider.GetRequiredService<BeatGridGuardrails>()));

        services.AddSingleton<AnalysisSanityValidator>();
        services.AddSingleton<ITrackAnalysisPipeline, TrackAnalysisPipeline>();
        services.AddSingleton<IRawAnalysisExporter, RawAnalysisJsonExporter>();
        services.AddSingleton<LoopAnalysisJsonExporter>();
        services.AddSingleton<AnalysisSummaryJsonExporter>();
        services.AddSingleton<AnalysisEngineCommand>();

        return services;
    }
}
