namespace EternalLoop.Contracts.Models;

public sealed class JukeboxAnalysisResult
{
    public required LoadedAudio Audio { get; init; }

    public required TrackAnalysis Analysis { get; init; }

    public required JukeboxGraph Graph { get; init; }

    public bool LoadedFromCache { get; init; }

    public required AiAnalysisRunInfo AiRun { get; init; }
}
