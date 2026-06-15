namespace EternalLoop.Core.Workflow;

public sealed record TrackInput
{
    private TrackInput(string filePath, string fileName)
    {
        FilePath = filePath;
        FileName = fileName;
    }

    public string FilePath { get; }

    public string FileName { get; }

    public static TrackInput FromFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Track file path cannot be empty.", nameof(filePath));
        }

        var normalizedPath = Path.GetFullPath(filePath);
        var fileName = Path.GetFileName(normalizedPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Track file path must include a file name.", nameof(filePath));
        }

        return new TrackInput(normalizedPath, fileName);
    }
}
