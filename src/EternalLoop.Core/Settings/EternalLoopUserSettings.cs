namespace EternalLoop.Core.Settings;

public sealed class EternalLoopUserSettings
{
    public int SettingsSchemaVersion { get; set; } = 4;

    public string Theme { get; set; } = "Dark";

    public LoopTuningSettings Tuning { get; set; } = LoopTuningSettings.Balanced();
}
