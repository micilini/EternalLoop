using System.Collections.Concurrent;
using System.Text.Json;
using EternalLoop.Core.Diagnostics;
using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.Core.Settings;

public sealed class JsonUserSettingsRepository : IUserSettingsRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SaveLocks = new(
        StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IAppPathProvider _pathProvider;
    private readonly IAppLogger _logger;

    public JsonUserSettingsRepository(IAppPathProvider pathProvider, IAppLogger? logger = null)
    {
        _pathProvider = pathProvider
            ?? throw new ArgumentNullException(nameof(pathProvider));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<EternalLoopUserSettings> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        _pathProvider.EnsureDirectories();

        if (!File.Exists(_pathProvider.SettingsFilePath))
        {
            return CreateDefault();
        }

        try
        {
            await using FileStream stream = File.OpenRead(_pathProvider.SettingsFilePath);
            EternalLoopUserSettings? settings = await JsonSerializer
                .DeserializeAsync<EternalLoopUserSettings>(
                    stream,
                    SerializerOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return Normalize(settings);
        }
        catch (JsonException exception)
        {
            BackupCorruptSettings(exception);
            EternalLoopUserSettings defaults = CreateDefault();
            await TrySaveDefaultsAsync(defaults, cancellationToken).ConfigureAwait(false);
            return defaults;
        }
        catch (IOException exception)
        {
            _logger.Log(AppLogLevel.Warning, "Settings could not be read. Defaults will be used.", exception);
            return CreateDefault();
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.Log(AppLogLevel.Warning, "Settings could not be accessed. Defaults will be used.", exception);
            return CreateDefault();
        }
    }

    public async Task SaveAsync(
        EternalLoopUserSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _pathProvider.EnsureDirectories();
        EternalLoopUserSettings normalized = Normalize(settings);
        string settingsPath = Path.GetFullPath(_pathProvider.SettingsFilePath);
        SemaphoreSlim saveLock = GetSaveLock(settingsPath);
        string? tempPath = null;

        await saveLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            tempPath = CreateTempPath(settingsPath);

            await using (FileStream stream = new(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await JsonSerializer
                    .SerializeAsync(stream, normalized, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(tempPath, settingsPath, overwrite: true);
            tempPath = null;
        }
        finally
        {
            TryDeleteTempFile(tempPath);
            saveLock.Release();
        }
    }

    private static SemaphoreSlim GetSaveLock(string settingsPath)
    {
        return SaveLocks.GetOrAdd(settingsPath, _ => new SemaphoreSlim(1, 1));
    }

    private static string CreateTempPath(string settingsPath)
    {
        return $"{settingsPath}.{Guid.NewGuid():N}.tmp";
    }

    private static void TryDeleteTempFile(string? tempPath)
    {
        if (string.IsNullOrWhiteSpace(tempPath) || !File.Exists(tempPath))
        {
            return;
        }

        try
        {
            File.Delete(tempPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static EternalLoopUserSettings CreateDefault()
    {
        return new EternalLoopUserSettings
        {
            Tuning = LoopTuningSettings.Balanced()
        };
    }

    private void BackupCorruptSettings(JsonException exception)
    {
        string? backupPath = CorruptFileBackup.TryCreate(_pathProvider.SettingsFilePath);
        _logger.Log(
            AppLogLevel.Warning,
            backupPath is null
                ? "settings.json is corrupt. Defaults will be used."
                : $"settings.json is corrupt. Backup created at {backupPath}.",
            exception);
    }

    private async Task TrySaveDefaultsAsync(
        EternalLoopUserSettings defaults,
        CancellationToken cancellationToken)
    {
        try
        {
            await SaveAsync(defaults, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.Log(AppLogLevel.Warning, "Default settings could not be saved.", exception);
        }
    }

    private static EternalLoopUserSettings Normalize(EternalLoopUserSettings? settings)
    {
        EternalLoopUserSettings normalized = settings ?? CreateDefault();
        int originalSchemaVersion = normalized.SettingsSchemaVersion;

        if (string.IsNullOrWhiteSpace(normalized.Theme))
        {
            normalized.Theme = "Dark";
        }

        if (originalSchemaVersion < 4
            && normalized.Tuning is not null
            && LooksLikeLegacyBalancedTuning(normalized.Tuning))
        {
            normalized.Tuning = LoopTuningSettings.Balanced();
        }

        normalized.SettingsSchemaVersion = Math.Max(4, normalized.SettingsSchemaVersion);
        normalized.Tuning = Normalize(normalized.Tuning);

        return normalized;
    }

    private static LoopTuningSettings Normalize(LoopTuningSettings? tuning)
    {
        if (tuning is null)
        {
            return LoopTuningSettings.Balanced();
        }

        LoopTuningPresetDefinition preset = LoopTuningPresetCatalog.GetById(tuning.Preset);
        tuning.Preset = preset.Id;
        tuning.SimilarityThreshold = Clamp(tuning.SimilarityThreshold, 0.65, 0.95);
        tuning.LookaheadDepth = Clamp(tuning.LookaheadDepth, 1, 5);
        tuning.MinJumpDistance = Clamp(tuning.MinJumpDistance, 4, 64);
        tuning.MaxBranchesPerBeat = Clamp(tuning.MaxBranchesPerBeat, 1, 12);
        tuning.JumpProbability = Clamp(tuning.JumpProbability, 0.0, 1.0);
        tuning.JumpCooldown = Clamp(tuning.JumpCooldown, 0, 64);
        tuning.FirstPassLinearPlaybackRatio = Clamp(
            tuning.FirstPassLinearPlaybackRatio,
            0.0,
            0.95);
        tuning.BranchQuantumType = string.IsNullOrWhiteSpace(tuning.BranchQuantumType)
            ? "beats"
            : tuning.BranchQuantumType;
        tuning.BranchMaxThreshold = BranchAnalysisTuningMapper.MapSimilarityToMaxThreshold(
            tuning.SimilarityThreshold);

        return tuning;
    }

    private static bool LooksLikeLegacyBalancedTuning(LoopTuningSettings tuning)
    {
        return string.Equals(
                tuning.Preset,
                LoopTuningPresetCatalog.BalancedId,
                StringComparison.OrdinalIgnoreCase)
            && NearlyEquals(tuning.SimilarityThreshold, 0.86)
            && tuning.LookaheadDepth == 4
            && tuning.MinJumpDistance == 20
            && tuning.MaxBranchesPerBeat == 4
            && tuning.BranchMaxThreshold == 80;
    }

    private static bool NearlyEquals(double left, double right)
    {
        return Math.Abs(left - right) < 0.0001;
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return minimum;
        }

        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
