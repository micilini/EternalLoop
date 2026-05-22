using System;

namespace EternalLoop.Contracts.Events;

public sealed class BeatChangedEventArgs : EventArgs
{
    public BeatChangedEventArgs(int beatIndex, double beatStart, double beatDuration)
    {
        BeatIndex = beatIndex;
        BeatStart = beatStart;
        BeatDuration = beatDuration;
    }

    public int BeatIndex { get; }

    public double BeatStart { get; }

    public double BeatDuration { get; }
}
