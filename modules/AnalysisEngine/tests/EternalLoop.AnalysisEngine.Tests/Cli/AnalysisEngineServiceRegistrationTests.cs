using EternalLoop.AnalysisEngine.Cli;
using EternalLoop.AnalysisEngine.Cli.Composition;
using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.BeatTracking;
using EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;
using EternalLoop.AnalysisEngine.Core.Features;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EternalLoop.AnalysisEngine.Tests.Cli;

public sealed class AnalysisEngineServiceRegistrationTests
{
    [Fact]
    public void AddAnalysisEngineServices_registers_core_pipeline_services()
    {
        var services = new ServiceCollection();

        services.AddAnalysisEngineServices();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IAudioLoader>().Should().BeOfType<NAudioAnalysisAudioLoader>();
        provider.GetRequiredService<IFeatureExtractor>().Should().BeOfType<NWavesAnalysisFeatureExtractor>();
        provider.GetRequiredService<IBeatTracker>().Should().BeOfType<BeatTrackerSelector>();
        provider.GetRequiredService<ITrackAnalysisPipeline>().Should().BeOfType<TrackAnalysisPipeline>();
        provider.GetRequiredService<AnalysisEngineCommand>().Should().NotBeNull();
    }

    [Fact]
    public void AddAnalysisEngineServices_registers_optional_beat_this_provider_dependencies()
    {
        var services = new ServiceCollection();

        services.AddAnalysisEngineServices();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<SpectralFluxBeatTracker>().Should().NotBeNull();
        provider.GetRequiredService<BeatGridGuardrails>().Should().NotBeNull();
        provider.GetRequiredService<BeatThisModelLocator>().Should().NotBeNull();
        provider.GetRequiredService<BeatThisPostprocessor>().Should().NotBeNull();
        provider.GetRequiredService<Func<string, IBeatModelRuntime>>().Should().NotBeNull();
        provider.GetRequiredService<BeatThisOnnxBeatTracker>().Should().NotBeNull();
    }
}