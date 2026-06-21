using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EternalLoop.AnalysisEngine.Core.BeatTracking.BeatThis;

public sealed class BeatThisDownbeatSanitizer
{
    public BeatThisDownbeatSanitizationResult Sanitize(
        IReadOnlyList<double> beats,
        IReadOnlyList<double> downbeats,
        double maxDistanceToNearestBeatSeconds)
    {
        if (!double.IsFinite(maxDistanceToNearestBeatSeconds) || maxDistanceToNearestBeatSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDistanceToNearestBeatSeconds), "Maximum distance must be finite and non-negative.");
        }

        if (downbeats == null || downbeats.Count == 0)
        {
            return new BeatThisDownbeatSanitizationResult
            {
                InputDownbeatCount = 0,
                OutputDownbeatCount = 0,
                Sanitized = false,
                MaxAllowedDistanceSeconds = maxDistanceToNearestBeatSeconds,
                Downbeats = Array.Empty<double>()
            };
        }

        if (!AreFinite(downbeats))
        {
            return DiscardDownbeats(
                downbeats.Count,
                reason: "downbeats-not-finite",
                warning: "beat-this-warning:downbeats-discarded:not-finite",
                maxDistanceToNearestBeatSeconds);
        }

        if (!IsStrictlyIncreasing(downbeats))
        {
            return DiscardDownbeats(
                downbeats.Count,
                reason: "downbeats-not-strictly-increasing",
                warning: "beat-this-warning:downbeats-discarded:not-strictly-increasing",
                maxDistanceToNearestBeatSeconds);
        }

        if (beats == null || beats.Count == 0)
        {
            return DiscardDownbeats(
                downbeats.Count,
                reason: "beats-missing",
                warning: "beat-this-warning:downbeats-discarded:beats-missing",
                maxDistanceToNearestBeatSeconds);
        }

        if (!AreFinite(beats))
        {
            return DiscardDownbeats(
                downbeats.Count,
                reason: "beats-not-finite",
                warning: "beat-this-warning:downbeats-discarded:beats-not-finite",
                maxDistanceToNearestBeatSeconds);
        }

        double maxDistance = 0.0;
        bool anyExceeded = false;

        foreach (var downbeat in downbeats)
        {
            double minDistance = double.MaxValue;
            foreach (var beat in beats)
            {
                double dist = Math.Abs(beat - downbeat);
                if (dist < minDistance)
                {
                    minDistance = dist;
                }
            }

            if (minDistance > maxDistance)
            {
                maxDistance = minDistance;
            }

            if (minDistance > maxDistanceToNearestBeatSeconds)
            {
                anyExceeded = true;
            }
        }

        if (anyExceeded)
        {
            string distanceStr = maxDistance.ToString("0.###", CultureInfo.InvariantCulture);
            return DiscardDownbeats(
                downbeats.Count,
                reason: "downbeat-not-aligned-to-beat",
                warning: $"beat-this-warning:downbeats-discarded:not-aligned-to-beat:{distanceStr}",
                maxDistanceToNearestBeatSeconds,
                maxDistance);
        }

        return new BeatThisDownbeatSanitizationResult
        {
            InputDownbeatCount = downbeats.Count,
            OutputDownbeatCount = downbeats.Count,
            Sanitized = false,
            MaxDistanceToNearestBeatSeconds = maxDistance,
            MaxAllowedDistanceSeconds = maxDistanceToNearestBeatSeconds,
            Downbeats = downbeats
        };
    }

    private static BeatThisDownbeatSanitizationResult DiscardDownbeats(
        int inputDownbeatCount,
        string reason,
        string warning,
        double maxAllowedDistanceSeconds,
        double? maxDistanceToNearestBeatSeconds = null)
    {
        return new BeatThisDownbeatSanitizationResult
        {
            InputDownbeatCount = inputDownbeatCount,
            OutputDownbeatCount = 0,
            Sanitized = true,
            Reason = reason,
            MaxDistanceToNearestBeatSeconds = maxDistanceToNearestBeatSeconds,
            MaxAllowedDistanceSeconds = maxAllowedDistanceSeconds,
            Warnings = new[] { warning },
            Downbeats = Array.Empty<double>()
        };
    }

    private static bool AreFinite(IReadOnlyList<double> values)
    {
        return values.All(double.IsFinite);
    }

    private static bool IsStrictlyIncreasing(IReadOnlyList<double> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] <= values[index - 1])
            {
                return false;
            }
        }

        return true;
    }
}
