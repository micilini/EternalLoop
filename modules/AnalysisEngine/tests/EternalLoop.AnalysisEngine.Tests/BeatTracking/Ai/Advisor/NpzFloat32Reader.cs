using System.IO.Compression;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai.Advisor;

public static class NpzFloat32Reader
{
    public static (float[] Data, int[] Shape) ReadDataArray(Stream npzStream)
    {
        var arrays = ReadAll(npzStream);

        if (arrays.TryGetValue("data", out var data))
        {
            return data;
        }

        if (arrays.Count == 1)
        {
            return arrays.Values.Single();
        }

        throw new InvalidDataException("NPZ did not contain a data.npy array.");
    }

    public static Dictionary<string, (float[] Data, int[] Shape)> ReadAll(Stream npzStream)
    {
        ArgumentNullException.ThrowIfNull(npzStream);

        using var archive = new ZipArchive(npzStream, ZipArchiveMode.Read, leaveOpen: true);
        var arrays = new Dictionary<string, (float[] Data, int[] Shape)>(StringComparer.Ordinal);

        foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".npy", StringComparison.OrdinalIgnoreCase)))
        {
            using var entryStream = entry.Open();
            var key = Path.GetFileNameWithoutExtension(entry.FullName);

            arrays[key] = NpyFloat32Reader.Read(entryStream);
        }

        return arrays;
    }
}
