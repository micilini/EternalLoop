using EternalLoop.AnalysisEngine.Core.Models;

namespace EternalLoop.AnalysisEngine.Core.Analysis;

public sealed record BarPhaseSelectionResult(
    int SelectedPhase,
    IReadOnlyList<BarPhaseCandidate> Candidates,
    string Mode);

public sealed record BarPhaseCandidate(
    int Phase,
    double Score,
    double DownbeatAccentScore,
    double BarRegularityScore,
    double PhraseStabilityScore,
    double BoundaryAlignmentScore);

public static class BarPhaseSelector
{
    private const double MinPhaseSwitchMargin = 0.05;

    public static BarPhaseSelectionResult Select(
        IReadOnlyList<Beat> beats,
        float[] onsetDetectionFunction,
        double framesPerSecond,
        int timeSignature)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(onsetDetectionFunction);

        if (beats.Count < timeSignature * 4 || onsetDetectionFunction.Length == 0 || framesPerSecond <= 0.0 || timeSignature <= 1)
        {
            return new BarPhaseSelectionResult(0, [new BarPhaseCandidate(0, 1.0, 1.0, 1.0, 1.0, 1.0)], "phase-zero");
        }

        var maxOdf = Math.Max(1e-9f, onsetDetectionFunction.Max());
        var noveltyPeaks = FindNoveltyPeaks(beats, onsetDetectionFunction, framesPerSecond, maxOdf);
        var candidates = new List<BarPhaseCandidate>();

        for (var phase = 0; phase < timeSignature; phase++)
        {
            var downbeats = Enumerable.Range(0, beats.Count)
                .Where(index => index % timeSignature == phase)
                .ToArray();
            var others = Enumerable.Range(0, beats.Count)
                .Where(index => index % timeSignature != phase)
                .ToArray();

            var downbeatAccent = RelativeMeanScore(beats, downbeats, others);
            var regularity = BarRegularity(beats, downbeats);
            var phrase = PhraseStability(beats, downbeats, timeSignature);
            var boundary = BoundaryAlignment(beats, downbeats, noveltyPeaks);
            var score =
                0.35 * downbeatAccent
                + 0.20 * regularity
                + 0.20 * phrase
                + 0.25 * boundary;
            candidates.Add(new BarPhaseCandidate(phase, score, downbeatAccent, regularity, phrase, boundary));
        }

        var phase0 = candidates.First(candidate => candidate.Phase == 0);
        var best = candidates.OrderByDescending(candidate => candidate.Score).First();
        var selected = best.Score >= phase0.Score + MinPhaseSwitchMargin ? best : phase0;
        return new BarPhaseSelectionResult(selected.Phase, candidates, "downbeat-selector");
    }

    private static double RelativeMeanScore(IReadOnlyList<Beat> beats, IReadOnlyList<int> downbeats, IReadOnlyList<int> others)
    {
        var downbeat = downbeats.Select(index => BeatAccent(beats[index])).DefaultIfEmpty(0.0).Average();
        var other = others.Select(index => BeatAccent(beats[index])).DefaultIfEmpty(0.0).Average();
        return Math.Clamp(0.5 + (downbeat - other), 0.0, 1.0);
    }

    private static double BeatAccent(Beat beat)
    {
        var loudness = beat.Loudness.Length > 0 ? beat.Loudness.Average(value => (double)value) : 0.0;
        return Math.Clamp(beat.Confidence * 0.75 + Math.Max(0.0, loudness) * 0.25, 0.0, 1.0);
    }

    private static double BarRegularity(IReadOnlyList<Beat> beats, IReadOnlyList<int> downbeats)
    {
        var starts = downbeats.Select(index => beats[index].Start).ToArray();
        if (starts.Length < 3)
        {
            return 0.0;
        }

        var intervals = starts.Zip(starts.Skip(1), (left, right) => right - left).Where(value => value > 0.0).ToArray();
        if (intervals.Length == 0)
        {
            return 0.0;
        }

        var mean = intervals.Average();
        var std = Math.Sqrt(intervals.Sum(value => Math.Pow(value - mean, 2.0)) / intervals.Length);
        return Math.Clamp(1.0 - std / Math.Max(1e-6, mean) / 0.08, 0.0, 1.0);
    }

    private static double PhraseStability(IReadOnlyList<Beat> beats, IReadOnlyList<int> downbeats, int timeSignature)
    {
        if (downbeats.Count < 8)
        {
            return 0.5;
        }

        var groups = downbeats.Chunk(4).Where(chunk => chunk.Length == 4).ToArray();
        if (groups.Length < 2)
        {
            return 0.5;
        }

        var changes = new List<double>();
        for (var index = 1; index < groups.Length; index++)
        {
            var previous = groups[index - 1].Select(beatIndex => BeatAccent(beats[beatIndex])).Average();
            var current = groups[index].Select(beatIndex => BeatAccent(beats[beatIndex])).Average();
            changes.Add(Math.Abs(current - previous));
        }

        return Math.Clamp(0.5 + changes.DefaultIfEmpty(0.0).Average(), 0.0, 1.0);
    }

    private static double BoundaryAlignment(IReadOnlyList<Beat> beats, IReadOnlyList<int> downbeats, IReadOnlyList<int> noveltyPeaks)
    {
        if (downbeats.Count == 0 || noveltyPeaks.Count == 0)
        {
            return 0.5;
        }

        var hits = downbeats.Count(index => noveltyPeaks.Any(peak => Math.Abs(peak - index) <= 1));
        return Math.Clamp(hits / (double)downbeats.Count * 2.0, 0.0, 1.0);
    }

    private static IReadOnlyList<int> FindNoveltyPeaks(
        IReadOnlyList<Beat> beats,
        IReadOnlyList<float> onsetDetectionFunction,
        double framesPerSecond,
        float maxOdf)
    {
        var values = new double[beats.Count];
        for (var index = 0; index < beats.Count; index++)
        {
            var frame = Math.Clamp((int)Math.Round(beats[index].Start * framesPerSecond), 0, onsetDetectionFunction.Count - 1);
            values[index] = onsetDetectionFunction[frame] / maxOdf;
        }

        var threshold = values.Average() + Math.Sqrt(values.Sum(value => Math.Pow(value - values.Average(), 2.0)) / Math.Max(1, values.Length)) * 0.5;
        var peaks = new List<int>();
        for (var index = 1; index < values.Length - 1; index++)
        {
            if (values[index] >= threshold && values[index] >= values[index - 1] && values[index] > values[index + 1])
            {
                peaks.Add(index);
            }
        }

        return peaks;
    }
}
