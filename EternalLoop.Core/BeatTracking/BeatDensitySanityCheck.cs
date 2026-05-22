namespace EternalLoop.Core.BeatTracking;

public static class BeatDensitySanityCheck
{
    public static bool IsSuspicious(
        double durationSeconds,
        double estimatedBpm,
        int beatCount)
    {
        if (durationSeconds <= 0 || estimatedBpm <= 0)
        {
            return false;
        }

        if (beatCount <= 0)
        {
            return true;
        }

        var expectedByTempo = durationSeconds * estimatedBpm / 60.0;
        var minimumExpected = Math.Max(8.0, expectedByTempo * 0.45);

        return beatCount < minimumExpected;
    }

    public static string Describe(
        double durationSeconds,
        double estimatedBpm,
        int beatCount)
    {
        var expectedByTempo = durationSeconds * estimatedBpm / 60.0;
        return $"beat density suspicious: {beatCount} beats, expected around {expectedByTempo:0} from {estimatedBpm:0.0} BPM over {durationSeconds:0.0}s";
    }
}
