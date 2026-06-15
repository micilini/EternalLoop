using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        try
        {
            byte[]? artworkBytes = Id3ArtworkReader.TryReadArtwork(filePath)
                ?? Mp4ArtworkReader.TryReadArtwork(filePath);

            return artworkBytes is null ? null : CreateFrozenImageSource(artworkBytes);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static ImageSource? CreateFrozenImageSource(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (FileFormatException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
