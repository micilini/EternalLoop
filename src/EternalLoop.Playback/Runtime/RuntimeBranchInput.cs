namespace EternalLoop.Playback.Runtime;

public sealed record RuntimeBranchInput(
    int Id,
    string Status,
    int FromBeat,
    int ToBeat,
    int JumpBeats,
    string Direction,
    double Distance,
    bool Deleted);
