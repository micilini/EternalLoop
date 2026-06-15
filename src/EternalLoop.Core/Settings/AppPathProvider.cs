namespace EternalLoop.Core.Settings;

public sealed class AppPathProvider : IAppPathProvider
{
    public AppPathProvider()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EternalLoop"))
    {
    }

    public AppPathProvider(string appDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(appDataDirectory))
        {
            throw new ArgumentException("App data directory cannot be empty.", nameof(appDataDirectory));
        }

        AppDataDirectory = Path.GetFullPath(appDataDirectory);
        CacheDirectory = Path.Combine(AppDataDirectory, "Cache");
        WorkflowCacheDirectory = Path.Combine(CacheDirectory, "Workflows");
        LogsDirectory = Path.Combine(AppDataDirectory, "Logs");
        SettingsFilePath = Path.Combine(AppDataDirectory, "settings.json");
        RecentTracksFilePath = Path.Combine(AppDataDirectory, "recent-tracks.json");
        RuntimeCacheIndexFilePath = Path.Combine(WorkflowCacheDirectory, "cache-index.json");
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
