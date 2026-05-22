using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;

namespace EternalLoop.Core.Analysis;

internal static class FeatureExtractionValidation
{
    public static void Validate(LoadedAudio audio, FeatureExtractionOptions options)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(options);

        if (audio.Samples.Length == 0)
        {
            throw new ArgumentException("Audio samples cannot be empty.", nameof(audio));
        }

        if (audio.SampleRate <= 0)
        {
            throw new ArgumentException("Audio sample rate must be greater than zero.", nameof(audio));
        }

        if (options.FrameSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Frame size must be greater than zero.");
        }

        if (options.HopLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Hop length must be greater than zero.");
        }

        if (options.HopLength > options.FrameSize)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Hop length cannot be greater than frame size.");
        }

        if (!IsPowerOfTwo(options.FrameSize))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Frame size must be a power of two.");
        }

        if (options.MfccCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MFCC count must be greater than zero.");
        }

        if (options.FilterBankSize < options.MfccCount)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Filter bank size must be greater than or equal to MFCC count.");
        }
    }

    private static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }
}
