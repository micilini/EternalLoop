using EternalLoop.AnalysisEngine.Core.Application;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Tests.TestData;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Application;

public sealed class AnalysisEngineServiceFactoryTests : IDisposable
{
    private readonly string _rootDirectory;

    public AnalysisEngineServiceFactoryTests()
    {
        _rootDirectory = Path.Combine(
            Path.GetTempPath(),
            "EternalLoopAnalysisEngineFactoryTests",
            Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void CreateDefaultShouldReturnAnalysisEngineService()
    {
        var service = AnalysisEngineServiceFactory.CreateDefault();

        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IAnalysisEngineService>();
    }

    [Fact]
    public async Task CreateDefault_with_auto_provider_analyzes_with_conservative_built_in()
    {
        var input = CreateInputWaveFile("auto-without-model.wav");
        var service = AnalysisEngineServiceFactory.CreateDefault();

        var result = await service.AnalyzeAsync(new AnalysisEngineRequest(
            input,
            new AnalysisOptions
            {
                BeatProvider = BeatTrackingProviderKind.Auto,
                AiFallbackMode = AiFallbackMode.FallbackToBuiltIn
            }));

        result.Analysis.Beats.Should().NotBeEmpty();
        result.Analysis.Diagnostics.Should().NotBeNull();
        result.Analysis.Diagnostics!.BeatProviderUsedAi.Should().BeFalse();
        result.Analysis.Diagnostics.BeatProviderUsedBuiltIn.Should().BeTrue();
        result.Analysis.Diagnostics.BeatProviderUsedFallback.Should().BeFalse();
        result.Analysis.Diagnostics.BeatProviderFallbackReason.Should().BeNull();
    }

    [Fact]
    public async Task CreateDefault_with_built_in_provider_does_not_attempt_beat_this()
    {
        var input = CreateInputWaveFile("built-in.wav");
        var service = AnalysisEngineServiceFactory.CreateDefault();

        var result = await service.AnalyzeAsync(new AnalysisEngineRequest(
            input,
            new AnalysisOptions
            {
                BeatProvider = BeatTrackingProviderKind.BuiltIn
            }));

        result.Analysis.Beats.Should().NotBeEmpty();
        result.Analysis.Diagnostics.Should().NotBeNull();
        result.Analysis.Diagnostics!.BeatProviderUsedAi.Should().BeFalse();
        result.Analysis.Diagnostics.BeatProviderUsedBuiltIn.Should().BeTrue();
        result.Analysis.Diagnostics.BeatProviderUsedFallback.Should().BeFalse();
        result.Analysis.Diagnostics.BeatProviderFallbackReason.Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private string CreateInputWaveFile(string fileName)
    {
        return TestWaveFileFactory.CreateSineWaveFile(
            _rootDirectory,
            fileName,
            sampleRate: TestSignalFactory.DefaultSampleRate,
            channels: 1,
            durationSeconds: 1.0);
    }
}
