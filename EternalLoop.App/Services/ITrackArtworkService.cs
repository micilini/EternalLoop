using System.Windows.Media;

namespace EternalLoop.App.Services;

public interface ITrackArtworkService
{
    ImageSource? TryLoadArtwork(string? filePath);

    string GetDisplayTitle(string? filePath);
}
