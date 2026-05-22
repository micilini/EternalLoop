using EternalLoop.Contracts.Models;
using EternalLoop.Core.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.Persistence;

public sealed class FileTrackAnalysisCacheTests
{
    [Fact]
    public async Task TryGetAsync_Should_ReturnNull_WhenCacheDoesNotExist()
    {
        var cache = CreateCache(out _);

        var result = await cache.TryGetAsync("missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ThenTryGetAsync_Should_ReturnAnalysis()
    {
        var cache = CreateCache(out _);
        var analysis = CreateAnalysis("hash");

        await cache.SaveAsync(analysis, CancellationToken.None);
        var result = await cache.TryGetAsync("hash", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Metadata.FileHash.Should().Be("hash");
        result.Beats.Should().HaveCount(2);
    }

    [Fact]
    public async Task TryGetAsync_Should_IgnoreOldSchema()
    {
        var cache = CreateCache(out _);
        await cache.SaveAsync(CreateAnalysis("hash", schemaVersion: "0.1"), CancellationToken.None);

        var result = await cache.TryGetAsync("hash", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetAsync_Should_IgnoreHashMismatch()
    {
        var cache = CreateCache(out var paths);
        await cache.SaveAsync(CreateAnalysis("actual"), CancellationToken.None);
        File.Move(
            Path.Combine(paths.CacheDirectory, "actual.json"),
            Path.Combine(paths.CacheDirectory, "requested.json"));

        var result = await cache.TryGetAsync("requested", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetAsync_Should_IgnoreCorruptJson()
    {
        var cache = CreateCache(out var paths);
        File.WriteAllText(Path.Combine(paths.CacheDirectory, "hash.json"), "{ broken");

        var result = await cache.TryGetAsync("hash", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_Should_ReturnCountAndBytes()
    {
        var cache = CreateCache(out _);
        await cache.SaveAsync(CreateAnalysis("one"), CancellationToken.None);
        await cache.SaveAsync(CreateAnalysis("two"), CancellationToken.None);

        var stats = await cache.GetStatsAsync(CancellationToken.None);

        stats.FileCount.Should().Be(2);
        stats.TotalBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ClearAsync_Should_DeleteCachedFiles()
    {
        var cache = CreateCache(out _);
        await cache.SaveAsync(CreateAnalysis("hash"), CancellationToken.None);

        await cache.ClearAsync();
        var stats = await cache.GetStatsAsync(CancellationToken.None);

        stats.FileCount.Should().Be(0);
    }

    private static FileTrackAnalysisCache CreateCache(out LocalAppDataPathProvider paths)
    {
        paths = new LocalAppDataPathProvider(Path.Combine(Path.GetTempPath(), "EternalLoopTests", Guid.NewGuid().ToString("N")));
        return new FileTrackAnalysisCache(paths, NullLogger<FileTrackAnalysisCache>.Instance);
    }

    private static TrackAnalysis CreateAnalysis(string hash, string schemaVersion = TrackAnalysis.CurrentSchemaVersion)
    {
        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = hash,
                FilePath = "test.mp3",
                DurationSeconds = 1.0,
                SampleRate = 22_050,
                Tempo = 120,
                TimeSignature = 4,
                SchemaVersion = schemaVersion
            },
            Segments = [],
            Beats =
            [
                new Beat { Index = 0, Start = 0, Duration = 0.5, Confidence = 1.0, Timbre = [1f], Pitches = [1f], Loudness = [0f, 0f, 0f], BarPosition = [0f, 1f] },
                new Beat { Index = 1, Start = 0.5, Duration = 0.5, Confidence = 1.0, Timbre = [1f], Pitches = [1f], Loudness = [0f, 0f, 0f], BarPosition = [1f, 0f] }
            ],
            Bars = [],
            Tatums = [],
            Sections = []
        };
    }
}
