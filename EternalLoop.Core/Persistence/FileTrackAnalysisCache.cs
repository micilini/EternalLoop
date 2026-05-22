using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EternalLoop.Core.Persistence;

public sealed class FileTrackAnalysisCache : ITrackAnalysisCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private readonly IAppPathProvider _paths;
    private readonly ILogger<FileTrackAnalysisCache> _logger;

    public FileTrackAnalysisCache(IAppPathProvider paths, ILogger<FileTrackAnalysisCache> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _paths.EnsureDirectories();
    }

    public async Task<TrackAnalysis?> TryGetAsync(string fileHash, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        _paths.EnsureDirectories();

        var path = GetCacheFilePath(fileHash);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var analysis = await JsonSerializer.DeserializeAsync<TrackAnalysis>(
                stream,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (analysis is null)
            {
                return null;
            }

            if (!StringComparer.OrdinalIgnoreCase.Equals(analysis.Metadata.FileHash, fileHash))
            {
                _logger.LogWarning("Ignoring cache {Path}: hash mismatch", path);
                return null;
            }

            if (!StringComparer.Ordinal.Equals(
                    analysis.Metadata.SchemaVersion,
                    TrackAnalysis.CurrentSchemaVersion))
            {
                _logger.LogInformation(
                    "Ignoring cache {Path}: schema {Schema} != current {CurrentSchema}",
                    path,
                    analysis.Metadata.SchemaVersion,
                    TrackAnalysis.CurrentSchemaVersion);
                return null;
            }

            return analysis;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ignoring corrupt cache file {Path}", path);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not read cache file {Path}", path);
            return null;
        }
    }

    public async Task SaveAsync(TrackAnalysis analysis, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        _paths.EnsureDirectories();

        var path = GetCacheFilePath(analysis.Metadata.FileHash);
        var tempPath = path + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                analysis,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    public Task<TrackAnalysisCacheStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        _paths.EnsureDirectories();

        var files = Directory.EnumerateFiles(_paths.CacheDirectory, "*.json").ToArray();
        long totalBytes = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                totalBytes += new FileInfo(file).Length;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not inspect cache file {Path}", file);
            }
        }

        return Task.FromResult(new TrackAnalysisCacheStats
        {
            FileCount = files.Length,
            TotalBytes = totalBytes
        });
    }

    public Task ClearAsync()
    {
        _paths.EnsureDirectories();

        foreach (var file in Directory.EnumerateFiles(_paths.CacheDirectory, "*.json"))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete cache file {Path}", file);
            }
        }

        return Task.CompletedTask;
    }

    private string GetCacheFilePath(string fileHash)
    {
        var safeHash = fileHash.Trim().ToLowerInvariant();
        return Path.Combine(_paths.CacheDirectory, safeHash + ".json");
    }
}
