using EternalLoop.Contracts.Enums;

namespace EternalLoop.Contracts.Options;

public sealed class PlaybackOptions
{
    public int CrossfadeMilliseconds { get; init; } = 20;

    public CrossfadeShape Shape { get; init; } = CrossfadeShape.EqualPower;
}
