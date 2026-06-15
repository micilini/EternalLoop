using System.Text;
using System.Text.Json;
using EternalLoop.AnalysisEngine.Core.Analysis;
using EternalLoop.AnalysisEngine.Core.Audio;
using EternalLoop.AnalysisEngine.Core.Export;
using EternalLoop.AnalysisEngine.Core.Export.LoopAnalysis;
using EternalLoop.AnalysisEngine.Core.Export.Summary;
using EternalLoop.AnalysisEngine.Core.Models;
using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Cli;

public sealed class AnalysisEngineCommand
{
    private const string OutputAlreadyExistsPrefix = "Output file already exists";

    private readonly ITrackAnalysisPipeline _pipeline;
    private readonly IRawAnalysisExporter _rawExporter;
    private readonly LoopAnalysisJsonExporter _loopAnalysisExporter;
    private readonly AnalysisSummaryJsonExporter _summaryExporter;
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public AnalysisEngineCommand(
        ITrackAnalysisPipeline pipeline,
        IRawAnalysisExporter rawExporter,
        LoopAnalysisJsonExporter loopAnalysisExporter,
        AnalysisSummaryJsonExporter summaryExporter)
        : this(pipeline, rawExporter, loopAnalysisExporter, summaryExporter, Console.Out, Console.Error)
    {
    }

    public AnalysisEngineCommand(
        ITrackAnalysisPipeline pipeline,
        IRawAnalysisExporter rawExporter,
        LoopAnalysisJsonExporter loopAnalysisExporter,
        AnalysisSummaryJsonExporter summaryExporter,
        TextWriter output,
        TextWriter error)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _rawExporter = rawExporter ?? throw new ArgumentNullException(nameof(rawExporter));
        _loopAnalysisExporter = loopAnalysisExporter ?? throw new ArgumentNullException(nameof(loopAnalysisExporter));
        _summaryExporter = summaryExporter ?? throw new ArgumentNullException(nameof(summaryExporter));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public async Task<int> ExecuteAsync(
        AnalysisEngineArguments arguments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        try
        {
            var inputPath = Path.GetFullPath(arguments.InputPath);

            if (!File.Exists(inputPath))
            {
                _error.WriteLine($"Input file not found: {inputPath}");
                return AnalysisEngineExitCodes.InputFileNotFound;
            }

            var outputDirectory = Path.GetFullPath(arguments.OutputDirectory);
            var progress = new ConsoleAnalysisProgressReporter(arguments.Quiet, _output);

            var analysis = await _pipeline
                .AnalyzeAsync(
                    inputPath,
                    new AnalysisOptions
                    {
                        Artist = arguments.Artist,
                        MusicalQuality = CreateMusicalQualityOptions(arguments)
                    },
                    progress,
                    cancellationToken)
                .ConfigureAwait(false);

            var rawResult = await ExportRawIfNeededAsync(
                    analysis,
                    arguments,
                    outputDirectory,
                    cancellationToken)
                .ConfigureAwait(false);

            var loopAnalysisResult = await ExportLoopAnalysisIfNeededAsync(
                    analysis,
                    arguments,
                    outputDirectory,
                    cancellationToken)
                .ConfigureAwait(false);

            var summaryResult = await ExportSummaryAsync(
                    analysis,
                    arguments,
                    outputDirectory,
                    rawResult,
                    loopAnalysisResult,
                    cancellationToken)
                .ConfigureAwait(false);

            await ExportDiagnosticsAsync(analysis, outputDirectory, arguments.Force, arguments.Pretty, cancellationToken)
                .ConfigureAwait(false);

            WriteSuccessSummary(
                arguments,
                outputDirectory,
                TrackAnalysisSummary.From(analysis),
                rawResult,
                loopAnalysisResult,
                summaryResult);

            return AnalysisEngineExitCodes.Success;
        }
        catch (AudioLoadingException exception)
        {
            _error.WriteLine(exception.Message);
            return AnalysisEngineExitCodes.AudioLoadFailed;
        }
        catch (InvalidOperationException exception)
        {
            _error.WriteLine(exception.Message);
            return AnalysisEngineExitCodes.ValidationFailed;
        }
        catch (IOException exception) when (exception.Message.StartsWith(OutputAlreadyExistsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _error.WriteLine(exception.Message);
            return AnalysisEngineExitCodes.OutputAlreadyExists;
        }
        catch (IOException exception)
        {
            _error.WriteLine(exception.Message);
            return AnalysisEngineExitCodes.ExportFailed;
        }
        catch (UnauthorizedAccessException exception)
        {
            _error.WriteLine(exception.Message);
            return AnalysisEngineExitCodes.ExportFailed;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _error.WriteLine(exception.Message);
            return AnalysisEngineExitCodes.AnalysisFailed;
        }
    }

    private static MusicalQualityOptions CreateMusicalQualityOptions(AnalysisEngineArguments arguments)
    {
        return new MusicalQualityOptions
        {
            AcousticSegmentation = arguments.MusicalQualitySegmentation,
            BeatMicroSnap = arguments.MusicalQualityBeatMicroSnap,
            AdaptiveTatums = arguments.MusicalQualityTatums,
            StructuralSections = arguments.MusicalQualitySections,
            EvidenceConfidences = arguments.MusicalQualityConfidences
        };
    }

    private static async Task ExportDiagnosticsAsync(
        TrackAnalysis analysis,
        string outputDirectory,
        bool force,
        bool pretty,
        CancellationToken cancellationToken)
    {
        if (analysis.Diagnostics is null)
        {
            return;
        }

        var outputPath = Path.Combine(outputDirectory, "analysis-diagnostics.json");
        if (File.Exists(outputPath) && !force)
        {
            throw new IOException($"Output file already exists: {outputPath}");
        }

        var json = JsonSerializer.Serialize(analysis.Diagnostics, JsonWriterOptionsFactory.Create(pretty));
        await File.WriteAllTextAsync(outputPath, json, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private async Task<ExportResult?> ExportRawIfNeededAsync(
        TrackAnalysis analysis,
        AnalysisEngineArguments arguments,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        if (arguments.Format is not AnalysisEngineFormat.Raw and not AnalysisEngineFormat.Both)
        {
            return null;
        }

        return await _rawExporter
            .ExportAsync(analysis, outputDirectory, arguments.Force, arguments.Pretty, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ExportResult?> ExportLoopAnalysisIfNeededAsync(
        TrackAnalysis analysis,
        AnalysisEngineArguments arguments,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        if (arguments.Format is not AnalysisEngineFormat.Loop and not AnalysisEngineFormat.Both)
        {
            return null;
        }

        return await _loopAnalysisExporter
            .ExportAsync(
                analysis,
                outputDirectory,
                arguments.TrackId,
                arguments.Title,
                arguments.Artist,
                arguments.Force,
                arguments.Pretty,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ExportResult> ExportSummaryAsync(
        TrackAnalysis analysis,
        AnalysisEngineArguments arguments,
        string outputDirectory,
        ExportResult? rawResult,
        ExportResult? loopAnalysisResult,
        CancellationToken cancellationToken)
    {
        return await _summaryExporter
            .ExportAsync(
                analysis,
                outputDirectory,
                rawResult,
                loopAnalysisResult,
                arguments.Force,
                arguments.Pretty,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private void WriteSuccessSummary(
        AnalysisEngineArguments arguments,
        string outputDirectory,
        TrackAnalysisSummary summary,
        ExportResult? rawResult,
        ExportResult? loopAnalysisResult,
        ExportResult summaryResult)
    {
        _output.WriteLine("Analysis exported successfully.");
        _output.WriteLine($"Output: {outputDirectory}");
        _output.WriteLine($"TrackId: {arguments.TrackId}");
        _output.WriteLine($"Title: {arguments.Title}");
        _output.WriteLine($"Artist: {arguments.Artist}");
        _output.WriteLine($"Tempo: {summary.Tempo:0.##} BPM");
        _output.WriteLine($"Duration: {summary.DurationSeconds:0.###}s");
        _output.WriteLine($"SampleRate: {summary.SampleRate}");
        _output.WriteLine($"Segments: {summary.SegmentCount}");
        _output.WriteLine($"Beats: {summary.BeatCount}");
        _output.WriteLine($"Bars: {summary.BarCount}");
        _output.WriteLine($"Tatums: {summary.TatumCount}");
        _output.WriteLine($"Sections: {summary.SectionCount}");

        if (rawResult is not null)
        {
            _output.WriteLine($"Raw: {rawResult.FilePath}");
        }

        if (loopAnalysisResult is not null)
        {
            _output.WriteLine($"Loop analysis: {loopAnalysisResult.FilePath}");
        }

        _output.WriteLine($"Summary: {summaryResult.FilePath}");
    }
}
