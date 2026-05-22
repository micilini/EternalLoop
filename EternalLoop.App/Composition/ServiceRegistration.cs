using EternalLoop.App.Navigation;
using EternalLoop.App.Services;
using EternalLoop.App.ViewModels;
using EternalLoop.App.Views;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using EternalLoop.Core.Analysis;
using EternalLoop.Core.Audio;
using EternalLoop.Core.BeatTracking;
using EternalLoop.Core.JukeboxEngine;
using EternalLoop.Core.Playback;
using EternalLoop.Core.Persistence;
using EternalLoop.Core.Similarity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EternalLoop.App.Composition;

internal static class ServiceRegistration
{
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<AppSessionState>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ITrackArtworkService, TrackArtworkService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAppPathProvider, LocalAppDataPathProvider>();
        services.AddSingleton<ITrackAnalysisCache, FileTrackAnalysisCache>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<ITuningService, TuningService>();
        services.AddSingleton<AiModelPathResolver>();
        services.AddSingleton<AiModelManifestLoader>();
        services.AddSingleton<IAiModelProvider>(provider => provider.GetRequiredService<AiModelManifestLoader>());
        services.AddSingleton<ILocalMusicEmbeddingModel, OnnxMusicEmbeddingModel>();
        services.AddSingleton<AiAudioPreprocessor>();
        services.AddSingleton<AiMelFilterBank>();
        services.AddSingleton<AiMelSpectrogramExtractor>();
        services.AddSingleton<AiPatchExtractor>();
        services.AddSingleton<AiPatchBatcher>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<SplashScreenWindow>();
        services.AddTransient<SplashScreenViewModel>();
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<AnalysisViewModel>();
        services.AddTransient<PlayerViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<RecentTracksViewModel>();

        services.AddOptions<AudioLoaderOptions>();
        services.AddSingleton<IAudioLoader, NAudioAudioLoader>();
        services.AddSingleton<IFeatureExtractor, NWavesFeatureExtractor>();
        services.AddSingleton<IBeatTracker, SpectralFluxBeatTracker>();
        services.AddSingleton<IBranchFinder, CosineSimilarityBranchFinder>();

        services.AddOptions<JukeboxEngineOptions>();
        services.AddSingleton<IJukeboxEngine, GraphTraversalJukeboxEngine>();
        services.AddSingleton<IJukeboxAnalysisPipeline, JukeboxAnalysisPipeline>();

        services.AddOptions<PlaybackOptions>();
        services.AddSingleton<IAudioPlayer, NAudioStreamPlayer>();
    }
}
