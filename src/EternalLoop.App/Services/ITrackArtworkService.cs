using System.Windows.Media;

namespace EternalLoop.App.Services;

public interface ITrackArtworkService
{
    string GetDisplayTitle(string filePath);

    ImageSource? TryLoadArtwork(string filePath);
}
