using System.IO;
using System.Windows.Media;

namespace EternalLoop.App.Services;

public sealed class TrackArtworkService : ITrackArtworkService
{
    public string GetDisplayTitle(string filePath)
    {
        string title = Path.GetFileNameWithoutExtension(filePath);

        return string.IsNullOrWhiteSpace(title)
            ? "Unknown track"
            : title;
    }

    public ImageSource? TryLoadArtwork(string filePath)
    {
        return null;
    }
}
