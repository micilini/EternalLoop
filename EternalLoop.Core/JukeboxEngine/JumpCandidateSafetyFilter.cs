using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.JukeboxEngine;

internal static class JumpCandidateSafetyFilter
{
    private enum RouteSafetyState
    {
        Unknown,
        Visiting,
        Safe,
        Unsafe
    }

    public static IReadOnlyList<JukeboxEdge> FilterSafeCandidates(
        int currentBeatIndex,
        IReadOnlyList<JukeboxEdge> candidates,
        JukeboxGraph graph,
        int beatCount,
        JukeboxEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        if (beatCount <= 0 || candidates.Count == 0)
        {
            return [];
        }

        return candidates
            .Where(edge => IsCandidateSafe(currentBeatIndex, edge, graph, beatCount, options))
            .ToArray();
    }

    public static bool IsCandidateSafe(
        int currentBeatIndex,
        JukeboxEdge edge,
        JukeboxGraph graph,
        int beatCount,
        JukeboxEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        if (beatCount <= 0)
        {
            return false;
        }

        if (edge.ToBeat < 0 || edge.ToBeat >= beatCount)
        {
            return false;
        }

        if (edge.ToBeat == currentBeatIndex)
        {
            return false;
        }

        return IsDestinationRouteSafe(
            edge.ToBeat,
            graph,
            beatCount,
            options,
            new Dictionary<int, RouteSafetyState>());
    }

    public static bool IsInEndGuardZone(int beatIndex, int beatCount, JukeboxEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (beatCount <= 1)
        {
            return false;
        }

        return beatIndex >= GetTerminalZoneStartIndex(beatCount, options);
    }

    public static bool IsLastSafeExitBeforeTerminal(
        int currentBeatIndex,
        JukeboxGraph graph,
        int beatCount,
        JukeboxEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        if (beatCount <= 0)
        {
            return false;
        }

        var terminalStart = GetTerminalZoneStartIndex(beatCount, options);
        if (currentBeatIndex >= terminalStart)
        {
            return true;
        }

        for (var beat = currentBeatIndex + 1; beat <= terminalStart; beat++)
        {
            if (!graph.JumpEdges.TryGetValue(beat, out var futureCandidates) || futureCandidates.Count == 0)
            {
                continue;
            }

            var safeFutureCandidates = FilterSafeCandidates(
                beat,
                futureCandidates,
                graph,
                beatCount,
                options);

            if (safeFutureCandidates.Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDestinationRouteSafe(
        int destinationBeat,
        JukeboxGraph graph,
        int beatCount,
        JukeboxEngineOptions options,
        Dictionary<int, RouteSafetyState> memo)
    {
        if (destinationBeat < 0 || destinationBeat >= beatCount)
        {
            return false;
        }

        if (memo.TryGetValue(destinationBeat, out var state))
        {
            return state is RouteSafetyState.Safe or RouteSafetyState.Visiting;
        }

        memo[destinationBeat] = RouteSafetyState.Visiting;

        var terminalStart = GetTerminalZoneStartIndex(beatCount, options);

        if (destinationBeat >= terminalStart)
        {
            var terminalSafe = HasTerminalEscapeRoute(destinationBeat, graph, beatCount, options, memo);
            memo[destinationBeat] = terminalSafe ? RouteSafetyState.Safe : RouteSafetyState.Unsafe;
            return terminalSafe;
        }

        for (var beat = destinationBeat; beat <= terminalStart; beat++)
        {
            if (!graph.JumpEdges.TryGetValue(beat, out var outgoingEdges) || outgoingEdges.Count == 0)
            {
                continue;
            }

            foreach (var outgoing in outgoingEdges)
            {
                if (!IsEdgeShapeValid(beat, outgoing, beatCount))
                {
                    continue;
                }

                if (outgoing.ToBeat < beat && !IsNearTerminalDestination(outgoing.ToBeat, beatCount, options))
                {
                    memo[destinationBeat] = RouteSafetyState.Safe;
                    return true;
                }

                if (IsDestinationRouteSafe(outgoing.ToBeat, graph, beatCount, options, memo))
                {
                    memo[destinationBeat] = RouteSafetyState.Safe;
                    return true;
                }
            }
        }

        memo[destinationBeat] = RouteSafetyState.Unsafe;
        return false;
    }

    private static bool IsNearTerminalDestination(
        int destinationBeat,
        int beatCount,
        JukeboxEngineOptions options)
    {
        if (beatCount <= 1)
        {
            return true;
        }

        return destinationBeat >= GetTerminalZoneStartIndex(beatCount, options);
    }

    private static bool HasTerminalEscapeRoute(
        int destinationBeat,
        JukeboxGraph graph,
        int beatCount,
        JukeboxEngineOptions options,
        Dictionary<int, RouteSafetyState> memo)
    {
        var lookahead = Math.Max(1, options.TerminalEscapeLookaheadBeats);
        var lastBeatToInspect = Math.Min(beatCount - 1, destinationBeat + lookahead);

        for (var beat = destinationBeat; beat <= lastBeatToInspect; beat++)
        {
            if (!graph.JumpEdges.TryGetValue(beat, out var outgoingEdges) || outgoingEdges.Count == 0)
            {
                continue;
            }

            foreach (var outgoing in outgoingEdges)
            {
                if (!IsEdgeShapeValid(beat, outgoing, beatCount))
                {
                    continue;
                }

                if (!IsNearTerminalDestination(outgoing.ToBeat, beatCount, options) &&
                    IsDestinationRouteSafe(outgoing.ToBeat, graph, beatCount, options, memo))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsEdgeShapeValid(int currentBeatIndex, JukeboxEdge edge, int beatCount)
    {
        return edge.ToBeat >= 0 &&
            edge.ToBeat < beatCount &&
            edge.ToBeat != currentBeatIndex;
    }

    private static int GetTerminalZoneStartIndex(int beatCount, JukeboxEngineOptions options)
    {
        if (beatCount <= 1)
        {
            return 0;
        }

        var ratio = Math.Clamp(options.EndGuardStartRatio, 0.0, 1.0);
        var ratioStart = (int)Math.Floor(beatCount * ratio);
        var minRemaining = Math.Clamp(
            options.MinimumBeatsBeforeEndForJumpDestination,
            0,
            Math.Max(0, beatCount - 1));
        var remainingStart = Math.Max(0, beatCount - 1 - minRemaining);

        return Math.Clamp(
            Math.Min(ratioStart, remainingStart),
            0,
            Math.Max(0, beatCount - 1));
    }
}
