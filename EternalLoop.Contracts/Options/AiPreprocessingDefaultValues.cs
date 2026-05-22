namespace EternalLoop.Contracts.Options;

public static class AiPreprocessingDefaultValues
{
    public const int SampleRate = AiModelDefaultValues.DiscogsEffNetSampleRate;
    public const int MelBands = AiModelDefaultValues.DiscogsEffNetMelBands;
    public const int PatchFrames = AiModelDefaultValues.DiscogsEffNetPatchFrames;
    public const int BatchSize = AiModelDefaultValues.DiscogsEffNetBatchSize;
    public const int FftSize = 512;
    public const int HopLength = 256;
    public const int PatchHopFrames = 48;
    public const double MinFrequencyHertz = 0.0;
    public const double LogFloor = 0.0000000001;
    public const double NormalizationEpsilon = 0.00000001;
}
