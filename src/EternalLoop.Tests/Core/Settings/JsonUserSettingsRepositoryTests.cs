using System.Text.Json;
using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Settings;

public sealed class JsonUserSettingsRepositoryTests
{
    [Fact]
    public async Task LoadAsyncShouldReturnBalancedWhenFileDoesNotExist()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings settings = await repository.LoadAsync();

        settings.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.BalancedId);
        settings.SettingsSchemaVersion.Should().Be(6);
        settings.Tuning.MaxBranchesPerBeat.Should().Be(6);
        settings.Tuning.JumpProbability.Should().Be(0.85);
        settings.Tuning.JumpCooldown.Should().Be(4);
        settings.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.10);
        settings.Tuning.AnalysisBeatProvider.Should().Be(AnalysisBeatModeCatalog.EnhancedId);
    }

    [Fact]
    public async Task SaveAsyncShouldPersistAndLoadChangedSettings()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var repository = new JsonUserSettingsRepository(paths.Provider);
        EternalLoopUserSettings settings = new()
        {
            Tuning = LoopTuningSettings.Balanced()
        };
        settings.Tuning.Preset = LoopTuningPresetCatalog.WildId;
        settings.Tuning.JumpProbability = 0.42;

        await repository.SaveAsync(settings);

        EternalLoopUserSettings loaded = await repository.LoadAsync();
        loaded.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.WildId);
        loaded.Tuning.JumpProbability.Should().Be(0.42);
    }

    [Fact]
    public async Task LoadAsyncShouldReturnBalancedForInvalidJson()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        paths.Provider.EnsureDirectories();
        await File.WriteAllTextAsync(paths.Provider.SettingsFilePath, "{ bad json");
        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings settings = await repository.LoadAsync();

        settings.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.BalancedId);
        settings.Tuning.MaxBranchesPerBeat.Should().Be(6);
        Directory.GetFiles(paths.Root, "settings.json.corrupt-*.bak").Should().ContainSingle();
        File.ReadAllText(paths.Provider.SettingsFilePath).Should().Contain("SettingsSchemaVersion");
    }

    [Fact]
    public async Task LoadAsyncShouldClampOutOfRangeValuesAndFallbackInvalidPreset()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var repository = new JsonUserSettingsRepository(paths.Provider);
        EternalLoopUserSettings settings = new()
        {
            Tuning = new LoopTuningSettings
            {
                Preset = "bad",
                SimilarityThreshold = 9,
                LookaheadDepth = 99,
                MinJumpDistance = -2,
                MaxBranchesPerBeat = 99,
                JumpProbability = -1,
                JumpCooldown = 100,
                FirstPassLinearPlaybackRatio = 2,
                BranchQuantumType = "",
                BranchMaxThreshold = 999
            }
        };

        await repository.SaveAsync(settings);
        EternalLoopUserSettings loaded = await repository.LoadAsync();

        loaded.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.BalancedId);
        loaded.Tuning.SimilarityThreshold.Should().Be(0.95);
        loaded.Tuning.LookaheadDepth.Should().Be(5);
        loaded.Tuning.MinJumpDistance.Should().Be(4);
        loaded.Tuning.MaxBranchesPerBeat.Should().Be(12);
        loaded.Tuning.JumpProbability.Should().Be(0);
        loaded.Tuning.JumpCooldown.Should().Be(64);
        loaded.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.95);
        loaded.Tuning.BranchQuantumType.Should().Be("beats");
        loaded.Tuning.BranchMaxThreshold.Should().Be(65);
    }

    [Fact]
    public async Task LoadAsyncShouldMigrateLegacyBalancedTuningToCurrentDefaults()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        paths.Provider.EnsureDirectories();

        string json = """
        {
          "SettingsSchemaVersion": 3,
          "Theme": "Dark",
          "Tuning": {
            "Preset": "Balanced",
            "SimilarityThreshold": 0.86,
            "LookaheadDepth": 4,
            "MinJumpDistance": 20,
            "MaxBranchesPerBeat": 4,
            "JumpProbability": 0.22,
            "JumpCooldown": 12,
            "FirstPassLinearPlaybackRatio": 0.78,
            "BranchQuantumType": "beats",
            "BranchMaxThreshold": 80,
            "AnalysisMusicalQuality": true
          }
        }
        """;

        await File.WriteAllTextAsync(paths.Provider.SettingsFilePath, json);

        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings loaded = await repository.LoadAsync();

        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.BalancedId);
        loaded.Tuning.SimilarityThreshold.Should().Be(0.86);
        loaded.Tuning.LookaheadDepth.Should().Be(1);
        loaded.Tuning.MinJumpDistance.Should().Be(4);
        loaded.Tuning.MaxBranchesPerBeat.Should().Be(6);
        loaded.Tuning.JumpProbability.Should().Be(0.85);
        loaded.Tuning.JumpCooldown.Should().Be(4);
        loaded.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.10);
        loaded.Tuning.BranchMaxThreshold.Should().Be(80);
    }

    [Fact]
    public async Task LoadAsyncShouldMigrateM18BalancedDefaultsToNewBalanced()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        await WriteSettingsAsync(paths, """
        {
          "SettingsSchemaVersion": 4,
          "Theme": "Dark",
          "Tuning": {
            "Preset": "Balanced",
            "SimilarityThreshold": 0.86,
            "LookaheadDepth": 1,
            "MinJumpDistance": 4,
            "MaxBranchesPerBeat": 4,
            "JumpProbability": 0.22,
            "JumpCooldown": 12,
            "FirstPassLinearPlaybackRatio": 0.78,
            "BranchQuantumType": "beats",
            "BranchMaxThreshold": 80,
            "AnalysisMusicalQuality": true
          }
        }
        """);
        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings loaded = await repository.LoadAsync();

        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.BalancedId);
        loaded.Tuning.MaxBranchesPerBeat.Should().Be(6);
        loaded.Tuning.JumpProbability.Should().Be(0.85);
        loaded.Tuning.JumpCooldown.Should().Be(4);
        loaded.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.10);
    }

    [Fact]
    public async Task LoadAsyncShouldMigrateM18WildDefaultsToNewWild()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        await WriteSettingsAsync(paths, """
        {
          "SettingsSchemaVersion": 4,
          "Theme": "Dark",
          "Tuning": {
            "Preset": "Wild",
            "SimilarityThreshold": 0.78,
            "LookaheadDepth": 1,
            "MinJumpDistance": 4,
            "MaxBranchesPerBeat": 6,
            "JumpProbability": 0.42,
            "JumpCooldown": 6,
            "FirstPassLinearPlaybackRatio": 0.70,
            "BranchQuantumType": "beats",
            "BranchMaxThreshold": 95,
            "AnalysisMusicalQuality": true
          }
        }
        """);
        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings loaded = await repository.LoadAsync();

        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.WildId);
        loaded.Tuning.MaxBranchesPerBeat.Should().Be(8);
        loaded.Tuning.JumpProbability.Should().Be(1.00);
        loaded.Tuning.JumpCooldown.Should().Be(0);
        loaded.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.00);
    }

    [Fact]
    public async Task LoadAsyncShouldMigrateM18ConservativeDefaultsToNewConservative()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        await WriteSettingsAsync(paths, """
        {
          "SettingsSchemaVersion": 4,
          "Theme": "Dark",
          "Tuning": {
            "Preset": "Conservative",
            "SimilarityThreshold": 0.92,
            "LookaheadDepth": 2,
            "MinJumpDistance": 16,
            "MaxBranchesPerBeat": 2,
            "JumpProbability": 0.14,
            "JumpCooldown": 16,
            "FirstPassLinearPlaybackRatio": 0.82,
            "BranchQuantumType": "beats",
            "BranchMaxThreshold": 70,
            "AnalysisMusicalQuality": true
          }
        }
        """);
        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings loaded = await repository.LoadAsync();

        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.ConservativeId);
        loaded.Tuning.MaxBranchesPerBeat.Should().Be(2);
        loaded.Tuning.JumpProbability.Should().Be(0.35);
        loaded.Tuning.JumpCooldown.Should().Be(12);
        loaded.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.50);
    }

    [Fact]
    public async Task LoadAsyncShouldPreserveCustomM18Settings()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        await WriteSettingsAsync(paths, """
        {
          "SettingsSchemaVersion": 4,
          "Theme": "Dark",
          "Tuning": {
            "Preset": "Balanced",
            "SimilarityThreshold": 0.86,
            "LookaheadDepth": 1,
            "MinJumpDistance": 4,
            "MaxBranchesPerBeat": 4,
            "JumpProbability": 0.33,
            "JumpCooldown": 12,
            "FirstPassLinearPlaybackRatio": 0.78,
            "BranchQuantumType": "beats",
            "BranchMaxThreshold": 80,
            "AnalysisMusicalQuality": true
          }
        }
        """);
        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings loaded = await repository.LoadAsync();

        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.Preset.Should().Be(LoopTuningPresetCatalog.BalancedId);
        loaded.Tuning.MaxBranchesPerBeat.Should().Be(4);
        loaded.Tuning.JumpProbability.Should().Be(0.33);
        loaded.Tuning.JumpCooldown.Should().Be(12);
        loaded.Tuning.FirstPassLinearPlaybackRatio.Should().Be(0.78);
    }

    [Fact]
    public async Task SaveAsyncShouldHandleConcurrentSavesOnSameRepository()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var repository = new JsonUserSettingsRepository(paths.Provider);
        EternalLoopUserSettings[] settings = CreateConcurrentSettings(50);

        Func<Task> saveAll = () => Task.WhenAll(settings.Select(setting => repository.SaveAsync(setting)));

        await saveAll.Should().NotThrowAsync();
        AssertSettingsFileIsValid(paths);
        AssertNoTemporarySettingsFiles(paths);

        EternalLoopUserSettings loaded = await repository.LoadAsync();
        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.Preset.Should().BeOneOf(
            LoopTuningPresetCatalog.ConservativeId,
            LoopTuningPresetCatalog.BalancedId,
            LoopTuningPresetCatalog.WildId);
    }

    [Fact]
    public async Task SaveAsyncShouldHandleConcurrentSavesAcrossRepositoryInstances()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        JsonUserSettingsRepository[] repositories =
        [
            new(paths.Provider),
            new(paths.Provider),
            new(paths.Provider)
        ];
        EternalLoopUserSettings[] settings = CreateConcurrentSettings(60);

        Func<Task> saveAll = () => Task.WhenAll(settings.Select((setting, index) =>
            repositories[index % repositories.Length].SaveAsync(setting)));

        await saveAll.Should().NotThrowAsync();
        AssertSettingsFileIsValid(paths);
        AssertNoTemporarySettingsFiles(paths);

        EternalLoopUserSettings loaded = await repositories[0].LoadAsync();
        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.Preset.Should().BeOneOf(
            LoopTuningPresetCatalog.ConservativeId,
            LoopTuningPresetCatalog.BalancedId,
            LoopTuningPresetCatalog.WildId);
    }

    [Fact]
    public async Task SaveAsyncShouldNotLeaveTemporarySettingsFilesAfterSuccessfulConcurrentSaves()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var repository = new JsonUserSettingsRepository(paths.Provider);

        await Task.WhenAll(CreateConcurrentSettings(30).Select(setting => repository.SaveAsync(setting)));

        AssertNoTemporarySettingsFiles(paths);
        Directory.GetFiles(paths.Root, "settings.json.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsyncShouldRemoveTemporaryFileWhenMoveFails()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var provider = new DirectorySettingsPathProvider(paths.Root);
        provider.EnsureDirectories();
        Directory.CreateDirectory(provider.SettingsFilePath);
        var repository = new JsonUserSettingsRepository(provider);

        Func<Task> save = () => repository.SaveAsync(CreateSettings(LoopTuningPresetCatalog.WildId, 0.42));

        await save.Should().ThrowAsync<Exception>()
            .Where(exception => exception is IOException || exception is UnauthorizedAccessException);
        Directory.GetFiles(paths.Root, "settings.json.*.tmp").Should().BeEmpty();
    }

    private static EternalLoopUserSettings[] CreateConcurrentSettings(int count)
    {
        string[] presets =
        [
            LoopTuningPresetCatalog.ConservativeId,
            LoopTuningPresetCatalog.BalancedId,
            LoopTuningPresetCatalog.WildId
        ];

        return Enumerable
            .Range(0, count)
            .Select(index => CreateSettings(presets[index % presets.Length], (index % 100) / 100.0))
            .ToArray();
    }

    private static EternalLoopUserSettings CreateSettings(string presetId, double jumpProbability)
    {
        LoopTuningSettings tuning = LoopTuningSettings.Balanced();
        LoopTuningPresetCatalog.ApplyPreset(tuning, LoopTuningPresetCatalog.GetById(presetId));
        tuning.JumpProbability = jumpProbability;

        return new EternalLoopUserSettings
        {
            Tuning = tuning
        };
    }

    private static void AssertSettingsFileIsValid(TempAppPaths paths)
    {
        string json = File.ReadAllText(paths.Provider.SettingsFilePath);
        using JsonDocument document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("SettingsSchemaVersion", out JsonElement schemaVersion).Should().BeTrue();
        schemaVersion.GetInt32().Should().Be(6);
    }

    private static void AssertNoTemporarySettingsFiles(TempAppPaths paths)
    {
        Directory.GetFiles(paths.Root, "settings.json.*.tmp").Should().BeEmpty();
    }

    private static async Task WriteSettingsAsync(TempAppPaths paths, string json)
    {
        paths.Provider.EnsureDirectories();
        await File.WriteAllTextAsync(paths.Provider.SettingsFilePath, json);
    }

    [Fact]
    public async Task LoadAsyncShouldNormalizeInvalidAnalysisBeatProviderToEnhanced()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        paths.Provider.EnsureDirectories();
        await File.WriteAllTextAsync(
            paths.Provider.SettingsFilePath,
            """
            {
              "SettingsSchemaVersion": 6,
              "Theme": "Dark",
              "Tuning": {
                "Preset": "Balanced",
                "SimilarityThreshold": 0.86,
                "LookaheadDepth": 1,
                "MinJumpDistance": 4,
                "MaxBranchesPerBeat": 6,
                "JumpProbability": 0.85,
                "JumpCooldown": 4,
                "FirstPassLinearPlaybackRatio": 0.10,
                "BranchQuantumType": "beats",
                "BranchMaxThreshold": 80,
                "AnalysisMusicalQuality": true,
                "AnalysisBeatProvider": "InvalidMode"
              }
            }
            """);
        var repository = new JsonUserSettingsRepository(paths.Provider);

        EternalLoopUserSettings loaded = await repository.LoadAsync();

        loaded.SettingsSchemaVersion.Should().Be(6);
        loaded.Tuning.AnalysisBeatProvider.Should().Be(AnalysisBeatModeCatalog.EnhancedId);
    }

    private sealed class TempAppPaths : IDisposable
    {
        private TempAppPaths(string root)
        {
            Root = root;
            Provider = new AppPathProvider(root);
        }

        public string Root { get; }

        public AppPathProvider Provider { get; }

        public static TempAppPaths Create()
        {
            return new TempAppPaths(Directory.CreateTempSubdirectory("eternalloop-settings-").FullName);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class DirectorySettingsPathProvider : IAppPathProvider
    {
        public DirectorySettingsPathProvider(string root)
        {
            AppDataDirectory = root;
            CacheDirectory = Path.Combine(root, "cache");
            WorkflowCacheDirectory = Path.Combine(root, "workflow-cache");
            LogsDirectory = Path.Combine(root, "logs");
            SettingsFilePath = Path.Combine(root, "settings.json");
            RecentTracksFilePath = Path.Combine(root, "recent-tracks.json");
            RuntimeCacheIndexFilePath = Path.Combine(root, "runtime-cache-index.json");
        }

        public string AppDataDirectory { get; }

        public string CacheDirectory { get; }

        public string WorkflowCacheDirectory { get; }

        public string LogsDirectory { get; }

        public string SettingsFilePath { get; }

        public string RecentTracksFilePath { get; }

        public string RuntimeCacheIndexFilePath { get; }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(AppDataDirectory);
            Directory.CreateDirectory(CacheDirectory);
            Directory.CreateDirectory(WorkflowCacheDirectory);
            Directory.CreateDirectory(LogsDirectory);
        }
    }
}
