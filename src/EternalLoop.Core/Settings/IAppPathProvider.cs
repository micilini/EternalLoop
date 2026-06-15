namespace EternalLoop.Core.Settings;

public interface IAppPathProvider
{
    string AppDataDirectory { get; }

    string CacheDirectory { get; }

    string WorkflowCacheDirectory { get; }

    string LogsDirectory { get; }

    string SettingsFilePath { get; }

    string RecentTracksFilePath { get; }

    string RuntimeCacheIndexFilePath { get; }

    void EnsureDirectories();
}
