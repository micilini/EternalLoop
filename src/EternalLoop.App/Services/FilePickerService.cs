using Microsoft.Win32;
using EternalLoop.Playback.Audio;

namespace EternalLoop.App.Services;

public sealed class FilePickerService : IFilePickerService
{
    public string? PickAudioFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an audio file",
            Filter = SupportedAudioFormats.DialogFilter,
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}
