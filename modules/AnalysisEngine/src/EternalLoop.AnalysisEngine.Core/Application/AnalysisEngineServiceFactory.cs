using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.Features;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Core.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.AnalysisEngine.Core.Application;

public static class AnalysisEngineServiceFactory
{
    public static IAnalysisEngineService CreateDefault(
        AudioLoaderOptions? audioLoaderOptions = null,
        ILoggerFactory? loggerFactory = null)
    {
        var pipeline = new TrackAnalysisPipeline(
            new NAudioAnalysisAudioLoader(audioLoaderOptions),
            new NWavesAnalysisFeatureExtractor(),
            new SpectralFluxBeatTracker(),
            new AnalysisSanityValidator(),
            loggerFactory?.CreateLogger<TrackAnalysisPipeline>() ?? NullLogger<TrackAnalysisPipeline>.Instance);

        return new AnalysisEngineService(pipeline);
    }
}
