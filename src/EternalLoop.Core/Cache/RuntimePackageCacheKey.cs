using System.Security.Cryptography;
using System.Text;
using EternalLoop.Core.Settings;

namespace EternalLoop.Core.Cache;

public static class RuntimePackageCacheKey
{
    public const int MappingVersion = 1;

    public static string Create(
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion)
    {
        return CreateRuntimeKey(identity, tuning, settingsSchemaVersion);
    }

    public static string CreateAnalysisKey(
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tuning);

        string fingerprint = string.Join(
            "|",
            "analysis",
            identity.Sha256,
            identity.FileSizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
            identity.LastWriteTimeUtc.ToBinary().ToString(System.Globalization.CultureInfo.InvariantCulture),
            settingsSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            MappingVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            tuning.AnalysisMusicalQuality);

        return Hash(fingerprint);
    }

    public static string CreateBranchKey(
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tuning);

        string fingerprint = string.Join(
            "|",
            "branches",
            CreateAnalysisKey(identity, tuning, settingsSchemaVersion),
            tuning.Preset,
            tuning.SimilarityThreshold.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            tuning.LookaheadDepth,
            tuning.MinJumpDistance,
            tuning.MaxBranchesPerBeat,
            tuning.BranchQuantumType,
            tuning.BranchMaxThreshold);

        return Hash(fingerprint);
    }

    public static string CreateRuntimeKey(
        TrackFileIdentity identity,
        LoopTuningSettings tuning,
        int settingsSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tuning);

        string fingerprint = string.Join(
            "|",
            "runtime",
            CreateBranchKey(identity, tuning, settingsSchemaVersion),
            tuning.JumpProbability.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            tuning.JumpCooldown,
            tuning.FirstPassLinearPlaybackRatio.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

        return Hash(fingerprint);
    }

    private static string Hash(string fingerprint)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
