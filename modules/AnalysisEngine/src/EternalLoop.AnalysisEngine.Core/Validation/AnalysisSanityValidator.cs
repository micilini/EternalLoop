using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Validation;

public sealed class AnalysisSanityValidator
{
    public AnalysisValidationResult Validate(TrackAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var errors = new List<string>();

        ValidateMetadata(analysis.Metadata, errors);
        ValidateSegments(analysis.Segments, errors);
        ValidateBeats(analysis.Beats, errors);
        ValidateBars(analysis.Bars, errors);
        ValidateTatums(analysis.Tatums, errors);
        ValidateSections(analysis.Sections, errors);

        return errors.Count == 0
            ? AnalysisValidationResult.Valid
            : AnalysisValidationResult.Invalid(errors);
    }

    private static void ValidateMetadata(TrackMetadata metadata, List<string> errors)
    {
        if (metadata.DurationSeconds <= 0.0)
        {
            errors.Add("Metadata duration must be greater than zero.");
        }

        if (metadata.SampleRate <= 0)
        {
            errors.Add("Metadata sample rate must be greater than zero.");
        }

        if (metadata.Tempo <= 0.0)
        {
            errors.Add("Metadata tempo must be greater than zero.");
        }

        if (metadata.TimeSignature <= 0)
        {
            errors.Add("Metadata time signature must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(metadata.FileHash))
        {
            errors.Add("Metadata file hash is required.");
        }

        if (string.IsNullOrWhiteSpace(metadata.FilePath))
        {
            errors.Add("Metadata file path is required.");
        }

        if (string.IsNullOrWhiteSpace(metadata.SchemaVersion))
        {
            errors.Add("Metadata schema version is required.");
        }
    }

    private static void ValidateSegments(IReadOnlyList<Segment> segments, List<string> errors)
    {
        if (segments.Count == 0)
        {
            errors.Add("Analysis must contain at least one segment.");
            return;
        }

        foreach (var segment in segments)
        {
            if (segment.Start < 0.0)
            {
                errors.Add("Segment start cannot be negative.");
            }

            if (segment.Duration <= 0.0)
            {
                errors.Add("Segment duration must be greater than zero.");
            }

            if (segment.Timbre.Length == 0)
            {
                errors.Add("Segment timbre cannot be empty.");
            }

            if (segment.Pitches.Length == 0)
            {
                errors.Add("Segment pitches cannot be empty.");
            }

            if (!IsFinite(segment.LoudnessStart) || !IsFinite(segment.LoudnessMax) || !IsFinite(segment.LoudnessMaxTime))
            {
                errors.Add("Segment loudness values must be finite.");
            }

            if (ContainsNonFinite(segment.Timbre))
            {
                errors.Add("Segment timbre values must be finite.");
            }

            if (ContainsNonFinite(segment.Pitches))
            {
                errors.Add("Segment pitch values must be finite.");
            }
        }
    }

    private static void ValidateBeats(IReadOnlyList<Beat> beats, List<string> errors)
    {
        if (beats.Count == 0)
        {
            errors.Add("Analysis must contain at least one beat.");
            return;
        }

        foreach (var beat in beats)
        {
            if (beat.Start < 0.0)
            {
                errors.Add("Beat start cannot be negative.");
            }

            if (beat.Duration <= 0.0)
            {
                errors.Add("Beat duration must be greater than zero.");
            }

            if (beat.Timbre.Length == 0)
            {
                errors.Add("Beat timbre cannot be empty.");
            }

            if (beat.Pitches.Length == 0)
            {
                errors.Add("Beat pitches cannot be empty.");
            }

            if (beat.Loudness.Length == 0)
            {
                errors.Add("Beat loudness cannot be empty.");
            }

            if (beat.BarPosition.Length == 0)
            {
                errors.Add("Beat bar position cannot be empty.");
            }

            if (ContainsNonFinite(beat.Timbre) || ContainsNonFinite(beat.Pitches) || ContainsNonFinite(beat.Loudness) || ContainsNonFinite(beat.BarPosition))
            {
                errors.Add("Beat feature values must be finite.");
            }
        }
    }

    private static void ValidateBars(IReadOnlyList<Bar> bars, List<string> errors)
    {
        if (bars.Count == 0)
        {
            errors.Add("Analysis must contain at least one bar.");
            return;
        }

        foreach (var bar in bars)
        {
            if (bar.Start < 0.0)
            {
                errors.Add("Bar start cannot be negative.");
            }

            if (bar.Duration <= 0.0)
            {
                errors.Add("Bar duration must be greater than zero.");
            }
        }
    }

    private static void ValidateTatums(IReadOnlyList<Tatum> tatums, List<string> errors)
    {
        if (tatums.Count == 0)
        {
            errors.Add("Analysis must contain at least one tatum.");
            return;
        }

        foreach (var tatum in tatums)
        {
            if (tatum.Start < 0.0)
            {
                errors.Add("Tatum start cannot be negative.");
            }

            if (tatum.Duration <= 0.0)
            {
                errors.Add("Tatum duration must be greater than zero.");
            }
        }
    }

    private static void ValidateSections(IReadOnlyList<Section> sections, List<string> errors)
    {
        if (sections.Count == 0)
        {
            errors.Add("Analysis must contain at least one section.");
            return;
        }

        foreach (var section in sections)
        {
            if (section.Start < 0.0)
            {
                errors.Add("Section start cannot be negative.");
            }

            if (section.Duration <= 0.0)
            {
                errors.Add("Section duration must be greater than zero.");
            }

            if (section.Tempo <= 0.0)
            {
                errors.Add("Section tempo must be greater than zero.");
            }
        }
    }

    private static bool ContainsNonFinite(float[] values)
    {
        return values.Any(value => !float.IsFinite(value));
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
