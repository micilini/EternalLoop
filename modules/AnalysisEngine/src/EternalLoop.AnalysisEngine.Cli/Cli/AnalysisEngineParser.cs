using EternalLoop.AnalysisEngine.Core.Options;

namespace EternalLoop.AnalysisEngine.Cli;

public static class AnalysisEngineParser
{
    private const string InputFlag = "--input";
    private const string OutputDirFlag = "--output-dir";
    private const string TrackIdFlag = "--track-id";
    private const string TitleFlag = "--title";
    private const string ArtistFlag = "--artist";
    private const string FormatFlag = "--format";
    private const string PrettyFlag = "--pretty";
    private const string ForceFlag = "--force";
    private const string QuietFlag = "--quiet";
    private const string MusicalQualityFlag = "--musical-quality";
    private const string MusicalQualitySegmentationFlag = "--mq-segmentation";
    private const string MusicalQualityBeatMicroSnapFlag = "--mq-beat-microsnap";
    private const string MusicalQualityTatumsFlag = "--mq-tatums";
    private const string MusicalQualitySectionsFlag = "--mq-sections";
    private const string MusicalQualityConfidencesFlag = "--mq-confidences";
    private const string HelpFlag = "--help";
    private const string DefaultTitle = "local-audio";

    public static AnalysisEngineParseResult Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0 || args.Any(argument => string.Equals(argument, HelpFlag, StringComparison.OrdinalIgnoreCase)))
        {
            return AnalysisEngineParseResult.Help();
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];

            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                return AnalysisEngineParseResult.Error($"Unexpected argument: {token}");
            }

            var separatorIndex = token.IndexOf('=', StringComparison.Ordinal);
            var name = separatorIndex >= 0 ? token[..separatorIndex] : token;
            var inlineValue = separatorIndex >= 0 ? token[(separatorIndex + 1)..] : null;

            if (IsBooleanFlag(name))
            {
                if (!string.IsNullOrEmpty(inlineValue))
                {
                    return AnalysisEngineParseResult.Error($"Flag {name} does not accept a value.");
                }

                flags.Add(name);
                continue;
            }

            if (!IsValueFlag(name))
            {
                return AnalysisEngineParseResult.Error($"Unknown option: {name}");
            }

            var value = inlineValue;

            if (value is null)
            {
                index++;

                if (index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    return AnalysisEngineParseResult.Error($"Missing value for {name}.");
                }

                value = args[index];
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return AnalysisEngineParseResult.Error($"Missing value for {name}.");
            }

            values[name] = value;
        }

        if (!values.TryGetValue(InputFlag, out var inputPath))
        {
            return AnalysisEngineParseResult.Error("Missing required option: --input.");
        }

        if (!values.TryGetValue(OutputDirFlag, out var outputDirectory))
        {
            return AnalysisEngineParseResult.Error("Missing required option: --output-dir.");
        }

        var titleFallback = ResolveTitleFallback(inputPath);
        var title = values.GetValueOrDefault(TitleFlag, titleFallback).Trim();
        var artist = values.GetValueOrDefault(ArtistFlag, AnalysisOptions.DefaultArtist).Trim();
        var trackIdSource = values.GetValueOrDefault(TrackIdFlag, titleFallback);

        var formatResult = ParseFormat(values.GetValueOrDefault(FormatFlag, "both"));

        if (formatResult is null)
        {
            return AnalysisEngineParseResult.Error("Invalid format. Expected raw, loop, or both.");
        }

        var arguments = new AnalysisEngineArguments
        {
            InputPath = inputPath,
            OutputDirectory = outputDirectory,
            TrackId = TrackIdNormalizer.Normalize(trackIdSource),
            Title = string.IsNullOrWhiteSpace(title) ? titleFallback : title,
            Artist = string.IsNullOrWhiteSpace(artist) ? AnalysisOptions.DefaultArtist : artist,
            Format = formatResult.Value,
            Pretty = true,
            Force = flags.Contains(ForceFlag),
            Quiet = flags.Contains(QuietFlag),
            MusicalQuality = flags.Contains(MusicalQualityFlag),
            MusicalQualitySegmentation = flags.Contains(MusicalQualityFlag) || flags.Contains(MusicalQualitySegmentationFlag),
            MusicalQualityBeatMicroSnap = flags.Contains(MusicalQualityFlag) || flags.Contains(MusicalQualityBeatMicroSnapFlag),
            MusicalQualityTatums = flags.Contains(MusicalQualityFlag) || flags.Contains(MusicalQualityTatumsFlag),
            MusicalQualitySections = flags.Contains(MusicalQualityFlag) || flags.Contains(MusicalQualitySectionsFlag),
            MusicalQualityConfidences = flags.Contains(MusicalQualityFlag) || flags.Contains(MusicalQualityConfidencesFlag)
        };

        return AnalysisEngineParseResult.Success(arguments);
    }

    private static bool IsBooleanFlag(string name)
    {
        return string.Equals(name, PrettyFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, ForceFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, QuietFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, MusicalQualityFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, MusicalQualitySegmentationFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, MusicalQualityBeatMicroSnapFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, MusicalQualityTatumsFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, MusicalQualitySectionsFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, MusicalQualityConfidencesFlag, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValueFlag(string name)
    {
        return string.Equals(name, InputFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, OutputDirFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, TrackIdFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, TitleFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, ArtistFlag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, FormatFlag, StringComparison.OrdinalIgnoreCase);
    }

    private static AnalysisEngineFormat? ParseFormat(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "raw" => AnalysisEngineFormat.Raw,
            "loop" => AnalysisEngineFormat.Loop,
            "both" => AnalysisEngineFormat.Both,
            _ => null
        };
    }

    private static string ResolveTitleFallback(string inputPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(inputPath.Replace('\\', '/'));

        return string.IsNullOrWhiteSpace(fileName) ? DefaultTitle : fileName;
    }
}
