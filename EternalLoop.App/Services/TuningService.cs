using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.App.Services;

public sealed class TuningService : ITuningService
{
    private readonly AppSessionState _sessionState;
    private readonly IJukeboxAnalysisPipeline _pipeline;
    private readonly IJukeboxEngine _engine;
    private readonly ISettingsRepository _settingsRepository;

    public TuningService(
        AppSessionState sessionState,
        IJukeboxAnalysisPipeline pipeline,
        IJukeboxEngine engine,
        ISettingsRepository settingsRepository)
    {
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    }

    public async Task<TuningApplyResult> ApplyAsync(CancellationToken cancellationToken)
    {
        var settings = _sessionState.Settings;
        var branchOptions = TuningOptionsMapper.ToBranchFindingOptions(settings);
        var engineOptions = TuningOptionsMapper.ToJukeboxEngineOptions(settings);

        _engine.UpdateOptions(engineOptions);

        var current = _sessionState.CurrentResult;
        if (current is null)
        {
            await _settingsRepository.SaveAsync(settings, cancellationToken).ConfigureAwait(false);

            return new TuningApplyResult
            {
                GraphReloaded = false,
                BranchCount = 0,
                Message = "Tuning saved. It will be applied to the next track."
            };
        }

        var graph = _pipeline.BuildGraph(current.Analysis.Beats, branchOptions);
        _engine.ReloadGraph(graph);

        _sessionState.CurrentResult = new JukeboxAnalysisResult
        {
            Audio = current.Audio,
            Analysis = current.Analysis,
            Graph = graph,
            LoadedFromCache = current.LoadedFromCache
        };

        await _settingsRepository.SaveAsync(settings, cancellationToken).ConfigureAwait(false);

        var branchCount = graph.JumpEdges.Sum(pair => pair.Value.Count);

        return new TuningApplyResult
        {
            GraphReloaded = true,
            BranchCount = branchCount,
            Message = $"Tuning applied. Loop graph rebuilt with {branchCount} branch(es)."
        };
    }
}
