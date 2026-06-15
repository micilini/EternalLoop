using System.Security.Cryptography;

namespace EternalLoop.Core.Cache;

public sealed class TrackFileIdentityService
{
    public async Task<TrackFileIdentity> CreateAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        string fullPath = Path.GetFullPath(filePath);
        var fileInfo = new FileInfo(fullPath);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Track file was not found.", fullPath);
        }

        await using FileStream stream = File.OpenRead(fullPath);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        string sha256 = Convert.ToHexString(hash).ToLowerInvariant();

        return new TrackFileIdentity(
            fullPath,
            fileInfo.Name,
            fileInfo.DirectoryName ?? string.Empty,
            fileInfo.Length,
            fileInfo.LastWriteTimeUtc,
            sha256);
    }
}
