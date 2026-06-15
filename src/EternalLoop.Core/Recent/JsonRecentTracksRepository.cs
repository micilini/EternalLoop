using System.Text.Json;
using EternalLoop.Core.Diagnostics;
using EternalLoop.Core.Settings;

namespace EternalLoop.Core.Recent;

public sealed class JsonRecentTracksRepository : IRecentTracksRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IAppPathProvider _pathProvider;
    private readonly IAppLogger _logger;

    public JsonRecentTracksRepository(IAppPathProvider pathProvider, IAppLogger? logger = null)
    {
        _pathProvider = pathProvider
            ?? throw new ArgumentNullException(nameof(pathProvider));
        _logger = logger ?? NullAppLogger.Instance;
    }

    public async Task<RecentTracksDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        _pathProvider.EnsureDirectories();

        if (!File.Exists(_pathProvider.RecentTracksFilePath))
        {
            return new RecentTracksDocument();
        }

        try
        {
            await using FileStream stream = File.OpenRead(_pathProvider.RecentTracksFilePath);
            return await JsonSerializer.DeserializeAsync<RecentTracksDocument>(
                    stream,
                    JsonOptions,
                    cancellationToken)
                    .ConfigureAwait(false)
                ?? new RecentTracksDocument();
        }
        catch (JsonException exception)
        {
            string? backupPath = CorruptFileBackup.TryCreate(_pathProvider.RecentTracksFilePath);
            _logger.Log(
                AppLogLevel.Warning,
                backupPath is null
                    ? "recent-tracks.json is corrupt. Recent tracks will be reset."
                    : $"recent-tracks.json is corrupt. Backup created at {backupPath}.",
                exception);
            return new RecentTracksDocument();
        }
        catch (IOException exception)
        {
            _logger.Log(AppLogLevel.Warning, "Recent tracks could not be read.", exception);
            return new RecentTracksDocument();
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.Log(AppLogLevel.Warning, "Recent tracks could not be accessed.", exception);
            return new RecentTracksDocument();
        }
    }

    public async Task SaveAsync(
        RecentTracksDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        _pathProvider.EnsureDirectories();
        await using FileStream stream = File.Create(_pathProvider.RecentTracksFilePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
