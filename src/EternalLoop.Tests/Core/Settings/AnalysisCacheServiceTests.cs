using EternalLoop.Core.Settings;
using FluentAssertions;

namespace EternalLoop.Tests.Core.Settings;

public sealed class AnalysisCacheServiceTests
{
    [Fact]
    public async Task GetStatsAsyncShouldCreateDirectoriesAndCountFiles()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var service = new AnalysisCacheService(paths.Provider);
        paths.Provider.EnsureDirectories();
        string nestedDirectory = Path.Combine(paths.Provider.CacheDirectory, "nested");
        Directory.CreateDirectory(nestedDirectory);
        await File.WriteAllBytesAsync(Path.Combine(paths.Provider.CacheDirectory, "a.bin"), [1, 2]);
        await File.WriteAllBytesAsync(Path.Combine(nestedDirectory, "b.bin"), [3, 4, 5]);

        AnalysisCacheStats stats = await service.GetStatsAsync();

        stats.FileCount.Should().Be(2);
        stats.TotalBytes.Should().Be(5);
        Directory.Exists(paths.Provider.WorkflowCacheDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task ClearAsyncShouldRemoveCacheContentsAndPreserveAppDataDirectory()
    {
        using TempAppPaths paths = TempAppPaths.Create();
        var service = new AnalysisCacheService(paths.Provider);
        paths.Provider.EnsureDirectories();
        await File.WriteAllTextAsync(Path.Combine(paths.Provider.CacheDirectory, "cached.json"), "{}");

        await service.ClearAsync();

        Directory.Exists(paths.Provider.AppDataDirectory).Should().BeTrue();
        Directory.Exists(paths.Provider.WorkflowCacheDirectory).Should().BeTrue();
        Directory.EnumerateFiles(paths.Provider.CacheDirectory, "*", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
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
            return new TempAppPaths(Directory.CreateTempSubdirectory("eternalloop-cache-").FullName);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
