namespace EternalLoop.Core.Settings;

public sealed class EternalLoopUserSettings
{
    public int SettingsSchemaVersion { get; set; } = 5;

    public string Theme { get; set; } = "Dark";

    public LoopTuningSettings Tuning { get; set; } = LoopTuningSettings.Balanced();
}
