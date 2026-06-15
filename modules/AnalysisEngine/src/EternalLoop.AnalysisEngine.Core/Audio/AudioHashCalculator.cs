using System.Security.Cryptography;

namespace EternalLoop.AnalysisEngine.Core.Audio;

public static class AudioHashCalculator
{
    public static async Task<string> Sha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        await using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();

        var hash = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
