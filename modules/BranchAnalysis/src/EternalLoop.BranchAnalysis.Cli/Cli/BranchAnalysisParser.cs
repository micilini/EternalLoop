using System.Globalization;
using EternalLoop.BranchAnalysis.Core.Config;
using EternalLoop.BranchAnalysis.Core.Runner;

namespace EternalLoop.BranchAnalysis.Cli.Cli;

public static class BranchAnalysisParser
{
    private const string SupportedQuantumType = "beats";
    private static readonly HashSet<string> BooleanFlags = new(StringComparer.Ordinal)
    {
        BranchAnalysisArguments.Force,
        BranchAnalysisArguments.Pretty,
        BranchAnalysisArguments.Quiet,
        BranchAnalysisArguments.DisableStructuralPolicy
    };

    private static readonly HashSet<string> ValueFlags = new(StringComparer.Ordinal)
    {
        BranchAnalysisArguments.AnalysisRoot,
        BranchAnalysisArguments.OutputRoot,
        BranchAnalysisArguments.QuantumType,
        BranchAnalysisArguments.SimilarityThreshold,
        BranchAnalysisArguments.LookaheadDepth,
        BranchAnalysisArguments.MinJumpDistance,
        BranchAnalysisArguments.MaxBranches,
        BranchAnalysisArguments.MaxThreshold
    };

    public static BranchAnalysisParseResult Parse(string[] args)
    {
        BranchAnalysisOptions options = BranchAnalysisOptions.CreateDefault();
        List<string> errors = [];
        bool maxThresholdExplicit = false;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];

            if (BranchAnalysisArguments.IsHelpFlag(argument))
            {
                return new BranchAnalysisParseResult
                {
                    Help = true,
                    Options = options,
                    Errors = Array.Empty<string>()
                };
            }

            if (BooleanFlags.Contains(argument))
            {
                ApplyBooleanFlag(options, argument);
                continue;
            }

            if (ValueFlags.Contains(argument))
            {
                string? value = index + 1 < args.Length ? args[index + 1] : null;

                if (value is null || value.StartsWith("--", StringComparison.Ordinal))
                {
                    errors.Add($"Missing value for {argument}");
                    continue;
                }

                if (string.Equals(argument, BranchAnalysisArguments.MaxThreshold, StringComparison.Ordinal))
                {
                    maxThresholdExplicit = true;
                }

                ApplyValueOption(options, argument, value, errors);
                index++;
                continue;
            }

            errors.Add($"Unknown argument: {argument}");
        }

        ValidateOptions(options, errors);

        if (!maxThresholdExplicit)
        {
            options.MaxThreshold = BranchAnalysisTuningMapper.MapSimilarityToMaxThreshold(
                options.SimilarityThreshold);
        }

        return new BranchAnalysisParseResult
        {
            Help = false,
            Options = options,
            Errors = errors
        };
    }

    private static void ApplyBooleanFlag(BranchAnalysisOptions options, string argument)
    {
        if (string.Equals(argument, BranchAnalysisArguments.Force, StringComparison.Ordinal))
        {
            options.Force = true;
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.Pretty, StringComparison.Ordinal))
        {
            options.Pretty = true;
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.Quiet, StringComparison.Ordinal))
        {
            options.Quiet = true;
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.DisableStructuralPolicy, StringComparison.Ordinal))
        {
            options.StructuralPolicy = false;
            options.AntiLocalLoopPolicy = false;
        }
    }

    private static void ApplyValueOption(BranchAnalysisOptions options, string argument, string value, List<string> errors)
    {
        if (string.Equals(argument, BranchAnalysisArguments.AnalysisRoot, StringComparison.Ordinal))
        {
            options.AnalysisRoot = value;
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.OutputRoot, StringComparison.Ordinal))
        {
            options.OutputRoot = value;
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.QuantumType, StringComparison.Ordinal))
        {
            options.QuantumType = value;
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.MaxBranches, StringComparison.Ordinal))
        {
            ApplyPositiveInteger(value, "maxBranches", parsed => options.MaxBranches = parsed, errors);
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.SimilarityThreshold, StringComparison.Ordinal))
        {
            ApplySimilarityThreshold(value, options, errors);
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.LookaheadDepth, StringComparison.Ordinal))
        {
            ApplyPositiveInteger(value, "lookaheadDepth", parsed => options.LookaheadDepth = parsed, errors);
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.MinJumpDistance, StringComparison.Ordinal))
        {
            ApplyPositiveInteger(value, "minJumpDistance", parsed => options.MinJumpDistance = parsed, errors);
            return;
        }

        if (string.Equals(argument, BranchAnalysisArguments.MaxThreshold, StringComparison.Ordinal))
        {
            ApplyPositiveInteger(value, "maxThreshold", parsed => options.MaxThreshold = parsed, errors);
        }
    }

    private static void ApplyPositiveInteger(
        string value,
        string optionName,
        Action<int> apply,
        List<string> errors)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed <= 0)
        {
            errors.Add($"{optionName} must be a positive integer");
            return;
        }

        apply(parsed);
    }

    private static void ApplySimilarityThreshold(
        string value,
        BranchAnalysisOptions options,
        List<string> errors)
    {
        if (!double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed)
            || !double.IsFinite(parsed)
            || parsed < BranchAnalysisTuningMapper.MinSimilarityThreshold
            || parsed > BranchAnalysisTuningMapper.MaxSimilarityThreshold)
        {
            errors.Add("similarityThreshold must be a number between 0.65 and 0.95");
            return;
        }

        options.SimilarityThreshold = parsed;
    }

    private static void ValidateOptions(BranchAnalysisOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.AnalysisRoot))
        {
            errors.Add("analysisRoot cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(options.OutputRoot))
        {
            errors.Add("outputRoot cannot be empty");
        }

        if (!string.Equals(options.QuantumType, SupportedQuantumType, StringComparison.Ordinal))
        {
            errors.Add($"Unsupported quantum type: {options.QuantumType}");
        }
    }
}
