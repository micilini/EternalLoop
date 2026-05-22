namespace EternalLoop.Contracts.Abstractions;

public interface IAppPathProvider
{
    string AppDataDirectory { get; }

    string CacheDirectory { get; }

    string LogsDirectory { get; }

    string SettingsFilePath { get; }

    void EnsureDirectories();
}
