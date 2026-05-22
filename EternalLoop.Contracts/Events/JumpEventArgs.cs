using System;

namespace EternalLoop.Contracts.Events;

public sealed class JumpEventArgs : EventArgs
{
    public JumpEventArgs(int fromBeat, int toBeat)
    {
        FromBeat = fromBeat;
        ToBeat = toBeat;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public int FromBeat { get; }

    public int ToBeat { get; }

    public DateTime OccurredAtUtc { get; }
}
