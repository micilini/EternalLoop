using System.Text;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai.Advisor;

public static class NpyFloat32Reader
{
    public static (float[] Data, int[] Shape) Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        var magic = reader.ReadBytes(6);

        if (magic.Length != 6
            || magic[0] != 0x93
            || magic[1] != (byte)'N'
            || magic[2] != (byte)'U'
            || magic[3] != (byte)'M'
            || magic[4] != (byte)'P'
            || magic[5] != (byte)'Y')
        {
            throw new InvalidDataException("Invalid NPY magic header.");
        }

        var major = reader.ReadByte();
        var minor = reader.ReadByte();
        int headerLength = major switch
        {
            1 => reader.ReadUInt16(),
            2 => checked((int)reader.ReadUInt32()),
            _ => throw new InvalidDataException($"Unsupported NPY version {major}.{minor}.")
        };
        var header = Encoding.ASCII.GetString(reader.ReadBytes(headerLength));

        if (!header.Contains("'descr': '<f4'", StringComparison.Ordinal)
            && !header.Contains("\"descr\": \"<f4\"", StringComparison.Ordinal)
            && !header.Contains("'descr': '|f4'", StringComparison.Ordinal)
            && !header.Contains("\"descr\": \"|f4\"", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Only little-endian float32 NPY arrays are supported.");
        }

        if (!header.Contains("'fortran_order': False", StringComparison.Ordinal)
            && !header.Contains("\"fortran_order\": False", StringComparison.Ordinal)
            && !header.Contains("\"fortran_order\": false", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Fortran-order NPY arrays are not supported.");
        }

        var shape = ParseShape(header);
        var valueCount = shape.Aggregate(1, checked((left, right) => left * right));
        var data = new float[valueCount];

        for (var index = 0; index < data.Length; index++)
        {
            data[index] = reader.ReadSingle();
        }

        return (data, shape);
    }

    private static int[] ParseShape(string header)
    {
        var shapeKey = header.IndexOf("'shape'", StringComparison.Ordinal);

        if (shapeKey < 0)
        {
            shapeKey = header.IndexOf("\"shape\"", StringComparison.Ordinal);
        }

        if (shapeKey < 0)
        {
            throw new InvalidDataException("NPY header did not contain shape.");
        }

        var open = header.IndexOf('(', shapeKey);
        var close = header.IndexOf(')', open + 1);

        if (open < 0 || close < 0)
        {
            throw new InvalidDataException("NPY shape tuple is invalid.");
        }

        return header[(open + 1)..close]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToArray();
    }
}
