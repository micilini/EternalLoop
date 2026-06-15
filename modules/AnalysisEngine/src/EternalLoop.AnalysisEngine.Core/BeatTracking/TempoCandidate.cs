namespace EternalLoop.AnalysisEngine.Core.BeatTracking;

public sealed record TempoCandidate(
    double Bpm,
    int Lag,
    double RawAutocorrelationScore,
    double PriorWeight,
    double ExtremeTempoPenalty,
    double FinalScore,
    string Origin,
    string HarmonicLabel);

public sealed record TempoCandidateSet(
    IReadOnlyList<TempoCandidate> Candidates,
    TempoCandidate? SelectedCandidate);
