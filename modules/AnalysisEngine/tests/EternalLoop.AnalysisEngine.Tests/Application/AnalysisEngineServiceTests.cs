using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Application;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;
using EternalLoop.AnalysisEngine.Core.Progress;
using FluentAssertions;

namespace EternalLoop.AnalysisEngine.Tests.Application;

public sealed class AnalysisEngineServiceTests
{
    [Fact]
    public async Task AnalyzeAsyncShouldCallPipelineAndReturnResult()
    {
        var analysis = CreateAnalysis();
        var pipeline = new FakeTrackAnalysisPipeline(analysis);
        var service = new AnalysisEngineService(pipeline);
        var request = new AnalysisEngineRequest("track.mp3");

        var result = await service.AnalyzeAsync(request, cancellationToken: CancellationToken.None);

        result.Analysis.Should().BeSameAs(analysis);
        result.Summary.BeatCount.Should().Be(1);
        pipeline.InputPath.Should().Be(request.InputPath);
        pipeline.Options.Should().BeSameAs(request.Options);
        pipeline.ProgressReporter.Should().BeSameAs(NullAnalysisProgressReporter.Instance);
    }

    [Fact]
    public async Task AnalyzeAsyncShouldUseExplicitProgressReporter()
    {
        var analysis = CreateAnalysis();
        var pipeline = new FakeTrackAnalysisPipeline(analysis);
        var service = new AnalysisEngineService(pipeline);
        var reporter = new CapturingAnalysisProgressReporter();
        var request = new AnalysisEngineRequest("track.mp3");

        await service.AnalyzeAsync(request, reporter, CancellationToken.None);

        pipeline.ProgressReporter.Should().BeSameAs(reporter);
    }

    [Fact]
    public async Task AnalyzeAsyncShouldRejectNullRequest()
    {
        var service = new AnalysisEngineService(new FakeTrackAnalysisPipeline(CreateAnalysis()));

        var act = async () => await service.AnalyzeAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void ConstructorShouldRejectNullPipeline()
    {
        var act = () => new AnalysisEngineService(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakeTrackAnalysisPipeline : ITrackAnalysisPipeline
    {
        private readonly TrackAnalysis _analysis;

        public FakeTrackAnalysisPipeline(TrackAnalysis analysis)
        {
            _analysis = analysis;
        }

        public string? InputPath { get; private set; }

        public AnalysisOptions? Options { get; private set; }

        public IAnalysisProgressReporter? ProgressReporter { get; private set; }

        public Task<TrackAnalysis> AnalyzeAsync(
            string inputPath,
            AnalysisOptions options,
            IAnalysisProgressReporter progressReporter,
            CancellationToken cancellationToken)
        {
            InputPath = inputPath;
            Options = options;
            ProgressReporter = progressReporter;

            return Task.FromResult(_analysis);
        }
    }

    private sealed class CapturingAnalysisProgressReporter : IAnalysisProgressReporter
    {
        public void Report(AnalysisStage stage, double progress01, string? message = null)
        {
        }
    }

    private static TrackAnalysis CreateAnalysis()
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "track.mp3",
                DurationSeconds = 60,
                SampleRate = 22050,
                Tempo = 120,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Beats =
            [
                new Beat
                {
                    Index = 0,
                    Start = 0,
                    Duration = 0.5,
                    Confidence = 1,
                    Timbre = [0],
                    Pitches = [0],
                    Loudness = [0],
                    BarPosition = [1, 0, 0, 0]
                }
            ],
            Bars = [],
            Tatums = [],
            Segments =
            [
                new Segment
                {
                    Start = 0,
                    Duration = 1,
                    Confidence = 1,
                    LoudnessStart = -10,
                    LoudnessMax = -5,
                    LoudnessMaxTime = 0.1,
                    Timbre = [0],
                    Pitches = [0]
                }
            ],
            Sections = []
        };
    }
}
