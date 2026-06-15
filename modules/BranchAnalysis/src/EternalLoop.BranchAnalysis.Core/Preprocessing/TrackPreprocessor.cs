using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Core.Preprocessing;

public static class TrackPreprocessor
{
    private const string SectionsType = "sections";
    private const string BarsType = "bars";
    private const string BeatsType = "beats";
    private const string TatumsType = "tatums";
    private const string SegmentsType = "segments";
    private const double FilterSegmentConfidenceThreshold = 0.3;
    private const double SimilarSegmentDistanceThreshold = 1.0;
    private const int TimbralDistanceValueCount = 3;

    public static TrackAnalysisDocument Preprocess(TrackAnalysisDocument track)
    {
        ValidateTrackShape(track);
        AssignQuantumNavigation(track);
        ConnectQuanta(track, SectionsType, BarsType);
        ConnectQuanta(track, BarsType, BeatsType);
        ConnectQuanta(track, BeatsType, TatumsType);
        ConnectQuanta(track, TatumsType, SegmentsType);
        ConnectFirstOverlappingSegment(track, BarsType);
        ConnectFirstOverlappingSegment(track, BeatsType);
        ConnectFirstOverlappingSegment(track, TatumsType);
        ConnectAllOverlappingSegments(track, BarsType);
        ConnectAllOverlappingSegments(track, BeatsType);
        ConnectAllOverlappingSegments(track, TatumsType);
        FilterSegments(track);

        return track;
    }

    public static void AssignQuantumNavigation(TrackAnalysisDocument track)
    {
        ValidateTrackShape(track);
        AssignNavigation(track, track.Analysis.Sections);
        AssignNavigation(track, track.Analysis.Bars);
        AssignNavigation(track, track.Analysis.Beats);
        AssignNavigation(track, track.Analysis.Tatums);
        AssignNavigation(track, track.Analysis.Segments);
    }

    public static void ConnectQuanta(TrackAnalysisDocument track, string parentType, string childType)
    {
        ValidateTrackShape(track);
        IReadOnlyList<TimeQuantum> parents = GetQuanta(track, parentType);
        IReadOnlyList<TimeQuantum> children = GetQuanta(track, childType);
        int last = 0;

        foreach (TimeQuantum parent in parents)
        {
            parent.Children = [];

            for (int index = last; index < children.Count; index++)
            {
                TimeQuantum child = children[index];

                if (child.Start >= parent.Start && child.Start < parent.End)
                {
                    child.Parent = parent;
                    child.IndexInParent = parent.Children.Count;
                    parent.Children.Add(child);
                    last = index;
                    continue;
                }

                if (child.Start > parent.Start)
                {
                    break;
                }
            }
        }
    }

    public static void ConnectFirstOverlappingSegment(TrackAnalysisDocument track, string quantumType)
    {
        ValidateTrackShape(track);
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, quantumType);
        List<SegmentQuantum> segments = track.Analysis.Segments;
        int last = 0;

        foreach (TimeQuantum quantum in quanta)
        {
            quantum.Oseg = null;

            for (int index = last; index < segments.Count; index++)
            {
                SegmentQuantum segment = segments[index];

                if (segment.Start >= quantum.Start)
                {
                    quantum.Oseg = segment;
                    last = index;
                    break;
                }
            }
        }
    }

    public static void ConnectAllOverlappingSegments(TrackAnalysisDocument track, string quantumType)
    {
        ValidateTrackShape(track);
        IReadOnlyList<TimeQuantum> quanta = GetQuanta(track, quantumType);
        List<SegmentQuantum> segments = track.Analysis.Segments;
        int last = 0;

        foreach (TimeQuantum quantum in quanta)
        {
            quantum.OverlappingSegments = [];

            for (int index = last; index < segments.Count; index++)
            {
                SegmentQuantum segment = segments[index];

                if (segment.End < quantum.Start)
                {
                    continue;
                }

                if (segment.Start > quantum.End)
                {
                    break;
                }

                last = index;
                quantum.OverlappingSegments.Add(segment);
            }
        }
    }

    public static void FilterSegments(TrackAnalysisDocument track)
    {
        if (track is null)
        {
            throw new TrackPreprocessorException("Track must be an object.");
        }

        if (track.Analysis is null)
        {
            throw new TrackPreprocessorException("Track analysis must be an object.");
        }

        if (track.Analysis.Segments is null)
        {
            throw new TrackPreprocessorException("Track analysis.segments must be a collection.");
        }

        List<SegmentQuantum> segments = track.Analysis.Segments;

        if (segments.Count == 0)
        {
            track.Analysis.FilteredSegments = [];
            return;
        }

        List<SegmentQuantum> filteredSegments = [segments[0]];

        for (int index = 1; index < segments.Count; index++)
        {
            SegmentQuantum segment = segments[index];
            SegmentQuantum lastSegment = filteredSegments[^1];

            if (IsSimilar(segment, lastSegment) && segment.Confidence < FilterSegmentConfidenceThreshold)
            {
                lastSegment.Duration += segment.Duration;
            }
            else
            {
                filteredSegments.Add(segment);
            }
        }

        track.Analysis.FilteredSegments = filteredSegments;
    }

    public static bool IsSimilar(SegmentQuantum segmentA, SegmentQuantum segmentB)
    {
        return TimbralDistance(segmentA, segmentB) < SimilarSegmentDistanceThreshold;
    }

    public static double TimbralDistance(SegmentQuantum segmentA, SegmentQuantum segmentB)
    {
        return EuclideanDistance(segmentA.Timbre, segmentB.Timbre, TimbralDistanceValueCount);
    }

    public static double EuclideanDistance(
        IReadOnlyList<double>? valuesA,
        IReadOnlyList<double>? valuesB,
        int valueCount)
    {
        if (valuesA is null || valuesB is null)
        {
            return double.PositiveInfinity;
        }

        if (valuesA.Count < valueCount || valuesB.Count < valueCount)
        {
            return double.PositiveInfinity;
        }

        double sum = 0;

        for (int index = 0; index < valueCount; index++)
        {
            double delta = ToFiniteNumber(valuesB[index]) - ToFiniteNumber(valuesA[index]);
            sum += delta * delta;
        }

        return Math.Sqrt(sum);
    }

    private static void ValidateTrackShape(TrackAnalysisDocument track)
    {
        if (track is null)
        {
            throw new TrackPreprocessorException("Track must be an object.");
        }

        if (track.Analysis is null)
        {
            throw new TrackPreprocessorException("Track analysis must be an object.");
        }

        RequireCollection(track.Analysis.Sections, "sections");
        RequireCollection(track.Analysis.Bars, "bars");
        RequireCollection(track.Analysis.Beats, "beats");
        RequireCollection(track.Analysis.Tatums, "tatums");
        RequireCollection(track.Analysis.Segments, "segments");
    }

    private static void RequireCollection<TQuantum>(IReadOnlyList<TQuantum>? quanta, string type)
    {
        if (quanta is null)
        {
            throw new TrackPreprocessorException($"Track analysis.{type} must be a collection.");
        }
    }

    private static void AssignNavigation<TQuantum>(TrackAnalysisDocument track, IList<TQuantum> quanta)
        where TQuantum : TimeQuantum
    {
        for (int index = 0; index < quanta.Count; index++)
        {
            TQuantum quantum = quanta[index];
            quantum.Track = track;
            quantum.Which = index;
            quantum.Prev = index > 0 ? quanta[index - 1] : null;
            quantum.Next = index < quanta.Count - 1 ? quanta[index + 1] : null;
        }
    }

    private static IReadOnlyList<TimeQuantum> GetQuanta(TrackAnalysisDocument track, string quantumType)
    {
        return quantumType switch
        {
            SectionsType => track.Analysis.Sections,
            BarsType => track.Analysis.Bars,
            BeatsType => track.Analysis.Beats,
            TatumsType => track.Analysis.Tatums,
            SegmentsType => track.Analysis.Segments,
            _ => throw new TrackPreprocessorException($"Unknown quantum type: {quantumType}")
        };
    }

    private static double ToFiniteNumber(double value)
    {
        return double.IsFinite(value) ? value : 0.0;
    }
}
