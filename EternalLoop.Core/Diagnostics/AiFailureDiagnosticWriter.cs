using System.Globalization;
using System.Text;
using EternalLoop.Contracts.Abstractions;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using Microsoft.Extensions.Logging;

namespace EternalLoop.Core.Diagnostics;

public sealed class AiFailureDiagnosticWriter
{
    private const string AiFailuresDirectoryName = "AiFailures";
    private const string LogExtension = ".log";
    private const int FileHashPrefixLength = 12;

    private readonly IAppPathProvider _paths;
    private readonly ILogger<AiFailureDiagnosticWriter> _logger;

    public AiFailureDiagnosticWriter(IAppPathProvider paths, ILogger<AiFailureDiagnosticWriter> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Write(
        string sourceFilePath,
        LoadedAudio audio,
        IReadOnlyList<Beat> beats,
        BranchFindingOptions branchOptions,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(branchOptions);
        ArgumentNullException.ThrowIfNull(exception);

        var directory = Path.Combine(_paths.LogsDirectory, AiFailuresDirectoryName);
        Directory.CreateDirectory(directory);

        var createdAtUtc = DateTime.UtcNow;
        var id = CreateId(createdAtUtc, audio.FileHash);
        var path = Path.Combine(directory, id + LogExtension);
        var text = BuildReport(id, createdAtUtc, sourceFilePath, audio, beats, branchOptions, exception);

        File.WriteAllText(path, text, Encoding.UTF8);

        _logger.LogWarning("AI failure diagnostic report written to {DiagnosticFilePath}", path);
        return path;
    }

    private static string CreateId(DateTime createdAtUtc, string fileHash)
    {
        var hash = string.IsNullOrWhiteSpace(fileHash)
            ? "nohash"
            : fileHash[..Math.Min(FileHashPrefixLength, fileHash.Length)];

        return $"ai-failure-{createdAtUtc:yyyyMMdd-HHmmss-fff}-{hash}";
    }

    private static string BuildReport(
        string id,
        DateTime createdAtUtc,
        string sourceFilePath,
        LoadedAudio audio,
        IReadOnlyList<Beat> beats,
        BranchFindingOptions branchOptions,
        Exception exception)
    {
        var builder = new StringBuilder();
        builder.AppendLine("EternalLoop AI Failure Diagnostic Report");
        builder.AppendLine("=======================================");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Id: {id}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"CreatedAtUtc: {createdAtUtc:O}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"CreatedAtLocal: {createdAtUtc.ToLocalTime():O}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"SourceFilePath: {sourceFilePath}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"FileHash: {audio.FileHash}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"DurationSeconds: {audio.DurationSeconds}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"SampleRate: {audio.SampleRate}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"SampleCount: {audio.Samples?.Length ?? 0}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"BeatCount: {beats.Count}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"UseAiSimilarity: {branchOptions.UseAiSimilarity}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"AiRejectionThreshold: {branchOptions.AiRejectionThreshold}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"AiPenaltyStartThreshold: {branchOptions.AiPenaltyStartThreshold}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"AiPenaltyStrength: {branchOptions.AiPenaltyStrength}");
        builder.AppendLine();
        builder.AppendLine("Exception");
        builder.AppendLine("---------");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.GetType().FullName}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
        builder.AppendLine();
        builder.AppendLine("Exception.ToString()");
        builder.AppendLine("--------------------");
        builder.AppendLine(exception.ToString());
        return builder.ToString();
    }
}
