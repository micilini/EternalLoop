namespace EternalLoop.Core.Settings;

public sealed class AnalysisCacheService : IAnalysisCacheService
{
    private readonly IAppPathProvider _pathProvider;

    public AnalysisCacheService(IAppPathProvider pathProvider)
    {
        _pathProvider = pathProvider
            ?? throw new ArgumentNullException(nameof(pathProvider));
    }

    public Task<AnalysisCacheStats> GetStatsAsync(
        CancellationToken cancellationToken = default)
    {
        _pathProvider.EnsureDirectories();

        int fileCount = 0;
        long totalBytes = 0;

        foreach (string filePath in Directory.EnumerateFiles(
            _pathProvider.CacheDirectory,
            "*",
            SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);
            fileCount++;
            totalBytes += fileInfo.Length;
        }

        return Task.FromResult(new AnalysisCacheStats(fileCount, totalBytes));
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _pathProvider.EnsureDirectories();

        foreach (string filePath in Directory.EnumerateFiles(
            _pathProvider.CacheDirectory,
            "*",
            SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(filePath);
        }

        foreach (string directoryPath in Directory
            .EnumerateDirectories(_pathProvider.CacheDirectory)
            .OrderByDescending(path => path.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }

        Directory.CreateDirectory(_pathProvider.WorkflowCacheDirectory);

        return Task.CompletedTask;
    }
}
