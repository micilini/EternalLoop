using EternalLoop.Contracts.Enums;
using System;

namespace EternalLoop.Contracts.Events;

public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackStateChangedEventArgs(PlaybackState oldState, PlaybackState newState, string? message = null)
    {
        OldState = oldState;
        NewState = newState;
        Message = message;
    }

    public PlaybackState OldState { get; }

    public PlaybackState NewState { get; }

    public string? Message { get; }
}
