using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EternalLoop.Core.JukeboxEngine;

public sealed class GraphTraversalJukeboxEngine : IJukeboxEngine
{
    private readonly ILogger<GraphTraversalJukeboxEngine> _logger;
    private readonly Random _random;
    private readonly object _syncRoot = new();
    private JukeboxEngineOptions _options;

    private TrackAnalysis? _analysis;
    private JukeboxGraph? _graph;
    private readonly Dictionary<(int FromBeat, int ToBeat), int> _repeatedJumpBlocks = new();
    private int[] _playCounts = [];
    private int _currentBeatIndex;
    private int _beatsSinceLastJump;
    private bool _firstPassGateReleased;

    public GraphTraversalJukeboxEngine(
        IOptions<JukeboxEngineOptions> options,
        ILogger<GraphTraversalJukeboxEngine> logger)
        : this(options.Value, logger, Random.Shared)
    {
    }

    internal GraphTraversalJukeboxEngine(
        JukeboxEngineOptions options,
        ILogger<GraphTraversalJukeboxEngine> logger,
        Random random)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public event EventHandler<JumpEventArgs>? JumpOccurred;

    public IReadOnlyList<Beat> Beats
    {
        get
        {
            lock (_syncRoot)
            {
                EnsureLoaded();
                return _analysis!.Beats;
            }
        }
    }

    public void Load(TrackAnalysis analysis, JukeboxGraph graph)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(graph);

        if (analysis.Beats.Count == 0)
        {
            throw new ArgumentException("Track analysis must contain at least one beat.", nameof(analysis));
        }

        lock (_syncRoot)
        {
            _analysis = analysis;
            _graph = graph;
            _playCounts = new int[analysis.Beats.Count];
            _currentBeatIndex = 0;
            _beatsSinceLastJump = 0;
            _firstPassGateReleased = false;
            _repeatedJumpBlocks.Clear();
        }

        _logger.LogInformation(
            "Jukebox engine loaded: {BeatCount} beats, {EdgeCount} jump edges",
            analysis.Beats.Count,
            graph.JumpEdges.Sum(pair => pair.Value.Count));
    }

    public void ReloadGraph(JukeboxGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        lock (_syncRoot)
        {
            EnsureLoaded();

            if (graph.Nodes.Count != _analysis!.Beats.Count)
            {
                throw new ArgumentException(
                    "Reloaded graph must contain the same beat count as the currently loaded analysis.",
                    nameof(graph));
            }

            _graph = graph;
            _repeatedJumpBlocks.Clear();
        }

        _logger.LogInformation(
            "Jukebox graph hot-reloaded: {EdgeCount} jump edges",
            graph.JumpEdges.Sum(pair => pair.Value.Count));
    }

    public void UpdateOptions(JukeboxEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (_syncRoot)
        {
            _options = options;

            if (_analysis is not null && IsFirstPassGateOpen())
            {
                _firstPassGateReleased = true;
            }
        }

        _logger.LogInformation(
            "Jukebox options hot-reloaded: probability={Probability}, cooldown={Cooldown}, firstPass={FirstPass}",
            options.JumpProbability,
            options.JumpCooldown,
            options.FirstPassLinearPlaybackRatio);
    }

    public int GetCurrentBeatIndex()
    {
        lock (_syncRoot)
        {
            EnsureLoaded();
            return _currentBeatIndex;
        }
    }

    public int PeekNextBeatIndex()
    {
        lock (_syncRoot)
        {
            EnsureLoaded();
            return ChooseNextBeat(previewOnly: true);
        }
    }

    public int AdvanceToNextBeat()
    {
        JumpEventArgs? jump = null;
        int nextBeat;

        lock (_syncRoot)
        {
            EnsureLoaded();

            var previousBeat = _currentBeatIndex;
            _playCounts[previousBeat]++;

            if (IsFirstPassGateOpen())
            {
                _firstPassGateReleased = true;
            }

            nextBeat = ChooseNextBeat(previewOnly: false);
            var linearNext = GetLinearNextBeat(previousBeat);
            var isJump = nextBeat != linearNext;
            var usedJump = isJump
                ? (FromBeat: previousBeat, ToBeat: nextBeat)
                : ((int FromBeat, int ToBeat)?)null;

            _currentBeatIndex = nextBeat;
            AdvanceRepeatedJumpBlocksForSource(previousBeat, usedJump);

            if (isJump)
            {
                BlockRepeatedJump(previousBeat, nextBeat);
                _beatsSinceLastJump = 0;
                jump = new JumpEventArgs(previousBeat, nextBeat);
            }
            else
            {
                _beatsSinceLastJump++;
            }
        }

        if (jump is not null)
        {
            _logger.LogDebug(
                "Jump occurred from beat {FromBeat} to beat {ToBeat}",
                jump.FromBeat,
                jump.ToBeat);

            JumpOccurred?.Invoke(this, jump);
        }

        return nextBeat;
    }

    public void SeekToBeat(int beatIndex)
    {
        lock (_syncRoot)
        {
            EnsureLoaded();

            var clamped = Math.Clamp(beatIndex, 0, _analysis!.Beats.Count - 1);
            _currentBeatIndex = clamped;
            _beatsSinceLastJump = 0;

            if (IsFirstPassGateOpen())
            {
                _firstPassGateReleased = true;
            }
        }
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            EnsureLoaded();
            Array.Clear(_playCounts);
            _currentBeatIndex = 0;
            _beatsSinceLastJump = 0;
            _firstPassGateReleased = false;
            _repeatedJumpBlocks.Clear();
        }
    }

    private int ChooseNextBeat(bool previewOnly)
    {
        var linearNext = GetLinearNextBeat(_currentBeatIndex);
        var safeCandidates = GetSafeCandidatesForCurrentBeat();

        if (ShouldForceSafeJumpForLoopSurvival(safeCandidates))
        {
            var forceCandidates = GetDiversityAwareCandidates(
                safeCandidates,
                allowBlockedFallback: true);

            return forceCandidates.Count == 0
                ? linearNext
                : ChooseFromCandidates(forceCandidates, previewOnly: false);
        }

        if (!CanConsiderJump(previewOnly))
        {
            return linearNext;
        }

        if (safeCandidates.Count == 0)
        {
            return linearNext;
        }

        var repeatAwareCandidates = GetDiversityAwareCandidates(
            safeCandidates,
            allowBlockedFallback: false);

        if (repeatAwareCandidates.Count == 0)
        {
            return linearNext;
        }

        return ChooseFromCandidates(repeatAwareCandidates, previewOnly);
    }

    private IReadOnlyList<JukeboxEdge> GetSafeCandidatesForCurrentBeat()
    {
        if (_analysis is null || _graph is null)
        {
            return [];
        }

        if (!_graph.JumpEdges.TryGetValue(_currentBeatIndex, out var candidates) || candidates.Count == 0)
        {
            return [];
        }

        var safeCandidates = JumpCandidateSafetyFilter.FilterSafeCandidates(
            _currentBeatIndex,
            candidates,
            _graph,
            _analysis.Beats.Count,
            _options);

        foreach (var unsafeCandidate in candidates.Except(safeCandidates))
        {
            _logger.LogDebug(
                "Unsafe jump candidate ignored: {FromBeat}->{ToBeat} because destination route has no escape before terminal zone",
                _currentBeatIndex,
                unsafeCandidate.ToBeat);
        }

        return safeCandidates;
    }

    private IReadOnlyList<JukeboxEdge> GetDiversityAwareCandidates(
        IReadOnlyList<JukeboxEdge> safeCandidates,
        bool allowBlockedFallback)
    {
        if (safeCandidates.Count == 0)
        {
            return [];
        }

        var blockedCandidates = safeCandidates
            .Where(IsRepeatedJumpBlocked)
            .ToArray();

        foreach (var blocked in blockedCandidates)
        {
            _logger.LogDebug(
                "Recently used jump avoided: {FromBeat}->{ToBeat}",
                _currentBeatIndex,
                blocked.ToBeat);
        }

        var filtered = safeCandidates
            .Where(edge => !IsRepeatedJumpBlocked(edge))
            .ToArray();

        if (filtered.Length > 0)
        {
            return filtered;
        }

        if (allowBlockedFallback && _options.AllowRepeatedJumpForTerminalEscape)
        {
            return safeCandidates;
        }

        return [];
    }

    private bool IsRepeatedJumpBlocked(JukeboxEdge edge)
    {
        return _repeatedJumpBlocks.TryGetValue((_currentBeatIndex, edge.ToBeat), out var remainingPasses) &&
            remainingPasses > 0;
    }

    private void BlockRepeatedJump(int fromBeat, int toBeat)
    {
        var cooldownPasses = Math.Max(0, _options.RepeatedJumpAvoidancePasses);
        if (cooldownPasses <= 0)
        {
            return;
        }

        _repeatedJumpBlocks[(fromBeat, toBeat)] = cooldownPasses;
    }

    private void AdvanceRepeatedJumpBlocksForSource(int sourceBeat, (int FromBeat, int ToBeat)? usedJump)
    {
        if (_repeatedJumpBlocks.Count == 0)
        {
            return;
        }

        var keysForSource = _repeatedJumpBlocks.Keys
            .Where(key => key.FromBeat == sourceBeat)
            .ToArray();

        foreach (var key in keysForSource)
        {
            if (usedJump.HasValue && key == usedJump.Value)
            {
                continue;
            }

            var nextValue = _repeatedJumpBlocks[key] - 1;
            if (nextValue <= 0)
            {
                _repeatedJumpBlocks.Remove(key);
            }
            else
            {
                _repeatedJumpBlocks[key] = nextValue;
            }
        }
    }

    private bool ShouldForceSafeJumpForLoopSurvival(IReadOnlyList<JukeboxEdge> safeCandidates)
    {
        if (!_options.ForceJumpInEndGuard || safeCandidates.Count == 0 || _analysis is null || _graph is null)
        {
            return false;
        }

        if (JumpCandidateSafetyFilter.IsInEndGuardZone(
            _currentBeatIndex,
            _analysis.Beats.Count,
            _options))
        {
            return true;
        }

        return JumpCandidateSafetyFilter.IsLastSafeExitBeforeTerminal(
            _currentBeatIndex,
            _graph,
            _analysis.Beats.Count,
            _options);
    }

    private int ChooseFromCandidates(IReadOnlyList<JukeboxEdge> candidates, bool previewOnly)
    {
        return _options.Strategy switch
        {
            JumpStrategy.LeastPlayed => JumpDecisionPolicy.ChooseLeastPlayed(
                candidates,
                _playCounts,
                _options.SteeringLookaheadDepth,
                _analysis?.Beats.Count ?? _playCounts.Length),
            JumpStrategy.Random => previewOnly
                ? JumpDecisionPolicy.ChooseHighestSimilarity(candidates)
                : JumpDecisionPolicy.ChooseRandom(candidates, _random),
            JumpStrategy.Weighted => previewOnly
                ? JumpDecisionPolicy.ChooseHighestSimilarity(candidates)
                : JumpDecisionPolicy.ChooseWeighted(candidates, _random),
            _ => GetLinearNextBeat(_currentBeatIndex)
        };
    }

    private bool CanConsiderJump(bool previewOnly)
    {
        if (_analysis is null)
        {
            return false;
        }

        if (!IsFirstPassGateOpen())
        {
            return false;
        }

        if (_currentBeatIndex < Math.Max(0, _options.MinBeatsBeforeFirstJump))
        {
            return false;
        }

        if (_beatsSinceLastJump < Math.Max(0, _options.JumpCooldown))
        {
            return false;
        }

        var probability = Math.Clamp(_options.JumpProbability, 0.0, 1.0);
        if (probability <= 0.0)
        {
            return false;
        }

        if (previewOnly)
        {
            return probability >= 1.0;
        }

        return _random.NextDouble() <= probability;
    }

    private bool IsFirstPassGateOpen()
    {
        if (_analysis is null)
        {
            return false;
        }

        if (_firstPassGateReleased)
        {
            return true;
        }

        var ratio = Math.Clamp(_options.FirstPassLinearPlaybackRatio, 0.0, 1.0);
        if (ratio <= 0.0)
        {
            return true;
        }

        var releaseBeat = Math.Clamp(
            (int)Math.Floor((_analysis.Beats.Count - 1) * ratio),
            0,
            Math.Max(0, _analysis.Beats.Count - 1));

        return _currentBeatIndex >= releaseBeat;
    }

    private int GetLinearNextBeat(int beatIndex)
    {
        if (_analysis is null || _analysis.Beats.Count == 0)
        {
            return 0;
        }

        return beatIndex + 1 >= _analysis.Beats.Count
            ? 0
            : beatIndex + 1;
    }

    private void EnsureLoaded()
    {
        if (_analysis is null || _graph is null || _playCounts.Length == 0)
        {
            throw new InvalidOperationException("Jukebox engine is not loaded.");
        }
    }
}
