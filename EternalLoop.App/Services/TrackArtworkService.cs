using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EternalLoop.App.Services;

public sealed class TrackArtworkService : ITrackArtworkService
{
    public ImageSource? TryLoadArtwork(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            var picture = tagFile.Tag.Pictures.FirstOrDefault();
            if (picture is null || picture.Data.Count == 0)
            {
                return null;
            }

            using var stream = new MemoryStream(picture.Data.Data);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    public string GetDisplayTitle(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "Loaded track";
        }

        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            if (!string.IsNullOrWhiteSpace(tagFile.Tag.Title))
            {
                return tagFile.Tag.Title.Trim();
            }
        }
        catch
        {
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }
}
