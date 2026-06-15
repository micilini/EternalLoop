using EternalLoop.Playback.Audio;

namespace EternalLoop.Core.Workflow;

public static class TrackWorkflowInputValidator
{
    public static TrackWorkflowError? Validate(TrackInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.FilePath))
        {
            return new TrackWorkflowError(
                "input_invalid",
                "Choose an audio file before starting analysis.");
        }

        if (!SupportedAudioFormats.IsSupportedExtension(input.FilePath))
        {
            return new TrackWorkflowError(
                "unsupported_audio_format",
                $"Choose an {SupportedAudioFormats.DisplayName} file.",
                input.FilePath);
        }

        if (!File.Exists(input.FilePath))
        {
            return new TrackWorkflowError(
                "input_not_found",
                "Track file was not found.",
                input.FilePath);
        }

        try
        {
            var fileInfo = new FileInfo(input.FilePath);

            if (fileInfo.Length == 0)
            {
                return new TrackWorkflowError(
                    "input_empty_file",
                    "The selected file is empty.",
                    input.FilePath);
            }

            using FileStream stream = File.Open(
                input.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }
        catch (UnauthorizedAccessException exception)
        {
            return new TrackWorkflowError(
                "input_access_denied",
                "EternalLoop could not access this file. Check permissions or try copying it to another folder.",
                exception.Message);
        }
        catch (IOException exception)
        {
            return new TrackWorkflowError(
                "input_locked_or_unreadable",
                "EternalLoop could not read this file. Close other apps using it and try again.",
                exception.Message);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            return new TrackWorkflowError(
                "input_invalid",
                "The selected file path is invalid.",
                exception.Message);
        }

        return null;
    }
}
