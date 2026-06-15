using EternalLoop.Playback.Audio;
using EternalLoop.Playback.Models;

namespace EternalLoop.Playback.Runtime;

public sealed class RuntimeLinearBeatIndex
{
    private readonly Dictionary<RuntimeBeat, int> _ordinalsByReference;

    private RuntimeLinearBeatIndex(IReadOnlyList<RuntimeBeat> beats)
    {
        Beats = beats;
        _ordinalsByReference = new Dictionary<RuntimeBeat, int>(ReferenceEqualityComparer.Instance);

        for (int index = 0; index < beats.Count; index++)
        {
            _ordinalsByReference[beats[index]] = index;
        }
    }

    public IReadOnlyList<RuntimeBeat> Beats { get; }

    public int Count => Beats.Count;

    public static RuntimeLinearBeatIndex FromFirstBeat(RuntimeBeat firstBeat)
    {
        ArgumentNullException.ThrowIfNull(firstBeat);

        List<RuntimeBeat> beats = [];
        HashSet<RuntimeBeat> visited = new(ReferenceEqualityComparer.Instance);
        RuntimeBeat? current = firstBeat;

        while (current is not null && visited.Add(current))
        {
            beats.Add(current);
            current = current.Next;
        }

        return new RuntimeLinearBeatIndex(beats);
    }

    public static RuntimeLinearBeatIndex FromTrack(RuntimeTrack track)
    {
        ArgumentNullException.ThrowIfNull(track);

        if (track.Beats.Count == 0)
        {
            throw new PlaybackException("Track must contain beats.");
        }

        return FromFirstBeat(track.Beats[0]);
    }

    public bool Contains(RuntimeBeat beat)
    {
        ArgumentNullException.ThrowIfNull(beat);
        return _ordinalsByReference.ContainsKey(beat);
    }

    public bool TryGetOrdinal(RuntimeBeat beat, out int ordinal)
    {
        ArgumentNullException.ThrowIfNull(beat);
        return _ordinalsByReference.TryGetValue(beat, out ordinal);
    }

    public int GetOrdinalOrWhich(RuntimeBeat beat)
    {
        ArgumentNullException.ThrowIfNull(beat);
        return TryGetOrdinal(beat, out int ordinal) ? ordinal : beat.Which;
    }
}
