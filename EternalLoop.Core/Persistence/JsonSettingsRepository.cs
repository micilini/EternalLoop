using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EternalLoop.Core.Persistence;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private const int CurrentSettingsSchemaVersion = TuningDefaultValues.SettingsSchemaVersion;
    private const int TuningSettingsSchemaVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly IAppPathProvider _paths;
    private readonly ILogger<JsonSettingsRepository> _logger;

    public JsonSettingsRepository(IAppPathProvider paths, ILogger<JsonSettingsRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _paths.EnsureDirectories();
    }

    public async Task<UserSettings> LoadAsync(CancellationToken cancellationToken)
    {
        _paths.EnsureDirectories();

        if (!File.Exists(_paths.SettingsFilePath))
        {
            return Normalize(new UserSettings());
        }

        try
        {
            var json = await File.ReadAllTextAsync(_paths.SettingsFilePath, cancellationToken).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);

            if (settings is not null && !HasSettingsSchemaVersion(json))
            {
                settings.SettingsSchemaVersion = 1;
            }

            return Normalize(settings ?? new UserSettings());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Settings file is corrupt; using defaults");
            return Normalize(new UserSettings());
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not read settings file; using defaults");
            return Normalize(new UserSettings());
        }
    }

    public async Task SaveAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _paths.EnsureDirectories();

        var normalized = Normalize(settings);
        var tempPath = _paths.SettingsFilePath + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                normalized,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, _paths.SettingsFilePath, overwrite: true);
    }

    private static UserSettings Normalize(UserSettings settings)
    {
        var shouldMigrateTuning = settings.SettingsSchemaVersion < TuningSettingsSchemaVersion;
        var shouldMigrateAi = settings.SettingsSchemaVersion < CurrentSettingsSchemaVersion;

        settings.Theme = settings.Theme switch
        {
            "Light" => "Light",
            "System" => "System",
            _ => "Dark"
        };

        settings.Volume = (float)TuningDefaultValues.DefaultVolume;
        settings.Preset = settings.Preset switch
        {
            "Conservative" => "Conservative",
            "Wild" => "Wild",
            _ => "Balanced"
        };

        if (shouldMigrateTuning)
        {
            var preset = TuningPresetCatalog.GetById(settings.Preset);
            TuningOptionsMapper.ApplyPreset(settings, preset);
        }

        if (shouldMigrateAi)
        {
            settings.UseAiSimilarity = TuningDefaultValues.UseAiSimilarity;
            settings.SettingsSchemaVersion = CurrentSettingsSchemaVersion;
        }

        settings.SimilarityThreshold = Math.Clamp(
            settings.SimilarityThreshold,
            TuningDefaultValues.MinProbability,
            TuningDefaultValues.MaxProbability);
        settings.LookaheadDepth = Math.Clamp(
            settings.LookaheadDepth,
            TuningDefaultValues.MinLookaheadDepth,
            TuningDefaultValues.MaxLookaheadDepth);
        settings.MinJumpDistance = Math.Clamp(
            settings.MinJumpDistance,
            TuningDefaultValues.MinJumpDistanceLimit,
            TuningDefaultValues.MaxJumpDistanceLimit);
        settings.MaxBranchesPerBeat = Math.Clamp(
            settings.MaxBranchesPerBeat,
            TuningDefaultValues.MinBranchesPerBeat,
            TuningDefaultValues.MaxBranchesPerBeatLimit);
        settings.JumpProbability = Math.Clamp(
            settings.JumpProbability,
            TuningDefaultValues.MinProbability,
            TuningDefaultValues.MaxProbability);
        settings.JumpCooldown = Math.Clamp(
            settings.JumpCooldown,
            TuningDefaultValues.MinJumpCooldown,
            TuningDefaultValues.MaxJumpCooldown);
        settings.FirstPassLinearPlaybackRatio = Math.Clamp(
            settings.FirstPassLinearPlaybackRatio,
            TuningDefaultValues.MinRatio,
            TuningDefaultValues.MaxRatio);
        settings.RecentFiles = RecentFileList.Normalize(settings.RecentFiles);

        if (string.IsNullOrWhiteSpace(settings.LastOpenedFile))
        {
            settings.LastOpenedFile = null;
        }

        return settings;
    }

    private static bool HasSettingsSchemaVersion(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty(nameof(UserSettings.SettingsSchemaVersion), out _);
    }
}
