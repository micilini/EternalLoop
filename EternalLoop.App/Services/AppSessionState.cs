using EternalLoop.Contracts.Models;

namespace EternalLoop.App.Services;

public sealed class AppSessionState
{
    public string? SelectedFilePath { get; set; }

    public JukeboxAnalysisResult? CurrentResult { get; set; }

    public bool ForceReanalysis { get; set; }

    public UserSettings Settings { get; set; } = new();

    public string EffectiveTheme { get; set; } = "Dark";

    public void ClearCurrentTrack()
    {
        SelectedFilePath = null;
        CurrentResult = null;
        ForceReanalysis = false;
    }

    public void RequestReanalysis(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        SelectedFilePath = filePath;
        CurrentResult = null;
        ForceReanalysis = true;
    }
}
