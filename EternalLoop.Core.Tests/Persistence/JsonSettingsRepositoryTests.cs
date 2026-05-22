using EternalLoop.Contracts.Models;
using EternalLoop.Core.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.Persistence;

public sealed class JsonSettingsRepositoryTests
{
    [Fact]
    public async Task LoadAsync_Should_ReturnDefaults_WhenSettingsFileDoesNotExist()
    {
        var repository = CreateRepository(out _);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.Theme.Should().Be("Dark");
        settings.Volume.Should().Be(1.0f);
        settings.SettingsSchemaVersion.Should().Be(3);
        settings.UseAiSimilarity.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_DefaultSettings_EnableAiSimilarity()
    {
        var repository = CreateRepository(out _);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.UseAiSimilarity.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_Should_PreserveSettings()
    {
        var repository = CreateRepository(out _);

        await repository.SaveAsync(new UserSettings
        {
            Theme = "Light",
            Volume = 0.42f,
            Preset = "Wild",
            SimilarityThreshold = 0.72,
            LookaheadDepth = 2,
            MinJumpDistance = 8,
            MaxBranchesPerBeat = 8,
            JumpProbability = 0.55,
            JumpCooldown = 4,
            FirstPassLinearPlaybackRatio = 0.65,
            UseAiSimilarity = false,
            RecentFiles = ["a.mp3", "b.mp3"]
        }, CancellationToken.None);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.Theme.Should().Be("Light");
        settings.Volume.Should().Be(1.0f);
        settings.Preset.Should().Be("Wild");
        settings.SimilarityThreshold.Should().Be(0.72);
        settings.LookaheadDepth.Should().Be(2);
        settings.MinJumpDistance.Should().Be(8);
        settings.MaxBranchesPerBeat.Should().Be(8);
        settings.JumpProbability.Should().Be(0.55);
        settings.JumpCooldown.Should().Be(4);
        settings.FirstPassLinearPlaybackRatio.Should().Be(0.65);
        settings.UseAiSimilarity.Should().BeFalse();
        settings.SettingsSchemaVersion.Should().Be(3);
        settings.RecentFiles.Should().Equal("a.mp3", "b.mp3");
    }

    [Fact]
    public async Task SaveAsync_Roundtrips_UseAiSimilarity_False()
    {
        var repository = CreateRepository(out _);

        await repository.SaveAsync(new UserSettings
        {
            UseAiSimilarity = false
        }, CancellationToken.None);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.UseAiSimilarity.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnDefaults_WhenJsonIsCorrupt()
    {
        var repository = CreateRepository(out var paths);
        File.WriteAllText(paths.SettingsFilePath, "{ broken");

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.Theme.Should().Be("Dark");
        settings.RecentFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_Should_NormalizeInvalidValues()
    {
        var repository = CreateRepository(out _);
        await repository.SaveAsync(new UserSettings
        {
            Theme = "Bad",
            Volume = 3f,
            SimilarityThreshold = 7,
            LookaheadDepth = -1,
            MinJumpDistance = -2,
            MaxBranchesPerBeat = 100,
            JumpProbability = 9,
            JumpCooldown = -10,
            FirstPassLinearPlaybackRatio = 5,
            RecentFiles = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11"]
        }, CancellationToken.None);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.Theme.Should().Be("Dark");
        settings.Volume.Should().Be(1f);
        settings.SimilarityThreshold.Should().Be(1.0);
        settings.LookaheadDepth.Should().Be(1);
        settings.MinJumpDistance.Should().Be(1);
        settings.MaxBranchesPerBeat.Should().Be(24);
        settings.JumpProbability.Should().Be(1.0);
        settings.JumpCooldown.Should().Be(0);
        settings.FirstPassLinearPlaybackRatio.Should().Be(0.95);
        settings.RecentFiles.Should().HaveCount(10);
    }

    [Fact]
    public async Task LoadAsync_Should_UseDefaults_ForOldSettingsWithoutTuningFields()
    {
        var repository = CreateRepository(out var paths);
        File.WriteAllText(paths.SettingsFilePath, """
            {
              "Theme": "Dark",
              "Volume": 0.5,
              "Preset": "Balanced"
            }
            """);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.SettingsSchemaVersion.Should().Be(3);
        settings.UseAiSimilarity.Should().BeTrue();
        settings.MinJumpDistance.Should().Be(20);
        settings.MaxBranchesPerBeat.Should().Be(3);
        settings.FirstPassLinearPlaybackRatio.Should().Be(0.78);
    }

    [Fact]
    public async Task LoadAsync_Should_MigrateOldTuningSettings_ToCurrentPresetDefaults()
    {
        var repository = CreateRepository(out var paths);
        await File.WriteAllTextAsync(paths.SettingsFilePath, """
            {
              "SettingsSchemaVersion": 1,
              "Preset": "Balanced",
              "SimilarityThreshold": 0.82,
              "LookaheadDepth": 3,
              "MinJumpDistance": 16,
              "MaxBranchesPerBeat": 5,
              "JumpProbability": 0.30,
              "JumpCooldown": 8,
              "FirstPassLinearPlaybackRatio": 0.75
            }
            """);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.SettingsSchemaVersion.Should().Be(3);
        settings.UseAiSimilarity.Should().BeTrue();
        settings.Preset.Should().Be("Balanced");
        settings.SimilarityThreshold.Should().Be(0.86);
        settings.LookaheadDepth.Should().Be(4);
        settings.MinJumpDistance.Should().Be(20);
        settings.MaxBranchesPerBeat.Should().Be(3);
        settings.JumpProbability.Should().Be(0.22);
        settings.JumpCooldown.Should().Be(12);
        settings.FirstPassLinearPlaybackRatio.Should().Be(0.78);
    }

    [Fact]
    public async Task LoadAsync_Should_MigrateSchemaTwoSettings_ToAiDefaultsWithoutResettingTuning()
    {
        var repository = CreateRepository(out var paths);
        await File.WriteAllTextAsync(paths.SettingsFilePath, """
            {
              "SettingsSchemaVersion": 2,
              "Preset": "Wild",
              "SimilarityThreshold": 0.72,
              "LookaheadDepth": 2,
              "MinJumpDistance": 8,
              "MaxBranchesPerBeat": 8,
              "JumpProbability": 0.55,
              "JumpCooldown": 4,
              "FirstPassLinearPlaybackRatio": 0.65
            }
            """);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.SettingsSchemaVersion.Should().Be(3);
        settings.UseAiSimilarity.Should().BeTrue();
        settings.Preset.Should().Be("Wild");
        settings.SimilarityThreshold.Should().Be(0.72);
        settings.LookaheadDepth.Should().Be(2);
        settings.MinJumpDistance.Should().Be(8);
        settings.MaxBranchesPerBeat.Should().Be(8);
        settings.JumpProbability.Should().Be(0.55);
        settings.JumpCooldown.Should().Be(4);
        settings.FirstPassLinearPlaybackRatio.Should().Be(0.65);
    }

    [Fact]
    public async Task LoadAsync_Migrates_MissingAiFlag_To_DefaultTrue()
    {
        var repository = CreateRepository(out var paths);
        await File.WriteAllTextAsync(paths.SettingsFilePath, """
            {
              "SettingsSchemaVersion": 2,
              "Preset": "Balanced"
            }
            """);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.SettingsSchemaVersion.Should().Be(3);
        settings.UseAiSimilarity.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_Should_PreserveAiToggle_WhenSchemaIsCurrent()
    {
        var repository = CreateRepository(out var paths);
        await File.WriteAllTextAsync(paths.SettingsFilePath, """
            {
              "SettingsSchemaVersion": 3,
              "UseAiSimilarity": false
            }
            """);

        var settings = await repository.LoadAsync(CancellationToken.None);

        settings.SettingsSchemaVersion.Should().Be(3);
        settings.UseAiSimilarity.Should().BeFalse();
    }

    private static JsonSettingsRepository CreateRepository(out LocalAppDataPathProvider paths)
    {
        paths = new LocalAppDataPathProvider(Path.Combine(Path.GetTempPath(), "EternalLoopTests", Guid.NewGuid().ToString("N")));
        return new JsonSettingsRepository(paths, NullLogger<JsonSettingsRepository>.Instance);
    }
}
