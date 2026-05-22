using EternalLoop.Contracts.Abstractions;

namespace EternalLoop.Core.Persistence;

public sealed class LocalAppDataPathProvider : IAppPathProvider
{
    public LocalAppDataPathProvider()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EternalLoop"))
    {
    }

    internal LocalAppDataPathProvider(string appDataDirectory)
    {
        AppDataDirectory = appDataDirectory ?? throw new ArgumentNullException(nameof(appDataDirectory));
        CacheDirectory = Path.Combine(AppDataDirectory, "Cache");
        LogsDirectory = Path.Combine(AppDataDirectory, "Logs");
        SettingsFilePath = Path.Combine(AppDataDirectory, "settings.json");
    }

    public string AppDataDirectory { get; }

    public string CacheDirectory { get; }

    public string LogsDirectory { get; }

    public string SettingsFilePath { get; }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
