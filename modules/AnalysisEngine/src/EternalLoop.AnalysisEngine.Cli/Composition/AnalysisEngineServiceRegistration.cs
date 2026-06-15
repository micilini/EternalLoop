using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Export;
using EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;
using EternalLoop.AnalysisEngine.Core.Export.Summary;
using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace EternalLoop.AnalysisEngine.Cli.Composition;

public static class AnalysisEngineServiceRegistration
{
    public static IServiceCollection AddAnalysisEngineServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAudioLoader, NAudioAnalysisAudioLoader>();
        services.AddSingleton<IFeatureExtractor, NWavesAnalysisFeatureExtractor>();
        services.AddSingleton<IBeatTracker, SpectralFluxBeatTracker>();
        services.AddSingleton<AnalysisSanityValidator>();
        services.AddSingleton<ITrackAnalysisPipeline, TrackAnalysisPipeline>();
        services.AddSingleton<IRawAnalysisExporter, RawAnalysisJsonExporter>();
        services.AddSingleton<LoopAnalysisJsonExporter>();
        services.AddSingleton<AnalysisSummaryJsonExporter>();
        services.AddSingleton<AnalysisEngineCommand>();

        return services;
    }
}
