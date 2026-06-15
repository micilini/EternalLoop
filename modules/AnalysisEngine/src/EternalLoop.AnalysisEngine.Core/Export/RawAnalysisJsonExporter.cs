using System.Text;
using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Export;

public sealed class RawAnalysisJsonExporter : IRawAnalysisExporter
{
    public const string DefaultFileName = "eternalloop-raw-analysis.json";

    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

    public async Task<ExportResult> ExportAsync(
        TrackAnalysis analysis,
        string outputDirectory,
        bool force,
        bool pretty,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        cancellationToken.ThrowIfCancellationRequested();

        var resolvedOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(resolvedOutputDirectory);

        var outputPath = Path.Combine(resolvedOutputDirectory, DefaultFileName);

        if (File.Exists(outputPath) && !force)
        {
            throw new IOException($"Output file already exists: {outputPath}");
        }

        var options = JsonWriterOptionsFactory.Create(pretty);
        var json = JsonSerializer.Serialize(analysis, options);

        await File.WriteAllTextAsync(outputPath, json, Utf8WithoutBom, cancellationToken).ConfigureAwait(false);

        var fileInfo = new FileInfo(outputPath);

        return new ExportResult
        {
            FilePath = outputPath,
            BytesWritten = fileInfo.Length
        };
    }
}
