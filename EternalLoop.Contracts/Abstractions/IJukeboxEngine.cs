using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using System;
using System.Collections.Generic;

namespace EternalLoop.Contracts.Abstractions;

public interface IJukeboxEngine
{
    event EventHandler<JumpEventArgs>? JumpOccurred;

    IReadOnlyList<Beat> Beats { get; }

    void Load(TrackAnalysis analysis, JukeboxGraph graph);

    void ReloadGraph(JukeboxGraph graph);

    void UpdateOptions(JukeboxEngineOptions options);

    int GetCurrentBeatIndex();

    int PeekNextBeatIndex();

    int AdvanceToNextBeat();

    void SeekToBeat(int beatIndex);

    void Reset();
}
