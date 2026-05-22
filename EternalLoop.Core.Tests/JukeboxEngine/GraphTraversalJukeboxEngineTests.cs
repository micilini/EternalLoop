using EternalLoop.Contracts.Enums;
using EternalLoop.Contracts.Events;
using EternalLoop.Contracts.Models;
using EternalLoop.Contracts.Options;
using EternalLoop.Core.JukeboxEngine;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalLoop.Core.Tests.JukeboxEngine;

public sealed class GraphTraversalJukeboxEngineTests
{
    [Fact]
    public void Load_Starts_At_Beat_Zero()
    {
        var engine = CreateEngine(new JukeboxEngineOptions());

        engine.Load(CreateAnalysis(4), CreateGraph(4));

        engine.GetCurrentBeatIndex().Should().Be(0);
    }

    [Fact]
    public void GetCurrentBeatIndex_Throws_Before_Load()
    {
        var engine = CreateEngine(new JukeboxEngineOptions());

        FluentActions.Invoking(() => engine.GetCurrentBeatIndex()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdvanceToNextBeat_Advances_Linearly_When_No_Edges()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(4), CreateGraph(4));

        engine.AdvanceToNextBeat().Should().Be(1);
        engine.AdvanceToNextBeat().Should().Be(2);
    }

    [Fact]
    public void AdvanceToNextBeat_Wraps_To_Zero_At_End()
    {
        var engine = CreateEngine(new JukeboxEngineOptions { JumpProbability = 0 });
        engine.Load(CreateAnalysis(2), CreateGraph(2));

        engine.AdvanceToNextBeat().Should().Be(1);
        engine.AdvanceToNextBeat().Should().Be(0);
    }

    [Fact]
    public void AdvanceToNextBeat_Does_Not_Jump_Before_MinBeatsBeforeFirstJump()
    {
        var engine = CreateEngine(new JukeboxEngineOptions { JumpProbability = 1, MinBeatsBeforeFirstJump = 2, JumpCooldown = 0 });
        engine.Load(CreateAnalysis(6), CreateGraph(6, Edge(0, 4, 1.0)));

        engine.AdvanceToNextBeat().Should().Be(1);
    }

    [Fact]
    public void AdvanceToNextBeat_Does_Not_Jump_During_Cooldown()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 2,
            FirstPassLinearPlaybackRatio = 0,
            EndGuardStartRatio = 1,
            MinimumBeatsBeforeEndForJumpDestination = 0
        });
        engine.Load(CreateAnalysis(8), CreateGraph(8, Edge(0, 4, 1.0), Edge(4, 1, 1.0)));

        engine.AdvanceToNextBeat().Should().Be(1);
        engine.AdvanceToNextBeat().Should().Be(2);
    }

    [Fact]
    public void AdvanceToNextBeat_Jumps_When_Probability_Is_One_And_Edge_Exists()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(6), CreateGraph(6, Edge(0, 4, 1.0), Edge(4, 1, 1.0)));

        engine.AdvanceToNextBeat().Should().Be(4);
    }

    [Fact]
    public void AdvanceToNextBeat_Raises_JumpOccurred_For_NonLinear_Jump()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        JumpEventArgs? raised = null;
        engine.JumpOccurred += (_, args) => raised = args;
        engine.Load(CreateAnalysis(6), CreateGraph(6, Edge(0, 4, 1.0), Edge(4, 1, 1.0)));

        engine.AdvanceToNextBeat();

        raised.Should().NotBeNull();
        raised!.FromBeat.Should().Be(0);
        raised.ToBeat.Should().Be(4);
    }

    [Fact]
    public void PeekNextBeatIndex_Does_Not_Change_Current_Beat()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(6), CreateGraph(6, Edge(0, 4, 1.0), Edge(4, 1, 1.0)));

        engine.PeekNextBeatIndex().Should().Be(4);
        engine.GetCurrentBeatIndex().Should().Be(0);
    }

    [Fact]
    public void PeekNextBeatIndex_Does_Not_Register_Repeated_Jump_Block()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(
            CreateAnalysis(20),
            CreateGraph(
                20,
                Edge(0, 10, 1.0),
                Edge(0, 12, 0.95),
                Edge(10, 0, 1.0),
                Edge(12, 0, 1.0)));

        engine.PeekNextBeatIndex().Should().Be(10);
        engine.PeekNextBeatIndex().Should().Be(10);

        engine.AdvanceToNextBeat().Should().Be(10);
    }

    [Fact]
    public void Reset_Returns_To_Beat_Zero_And_Clears_State()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            Strategy = JumpStrategy.LeastPlayed,
            FirstPassLinearPlaybackRatio = 0,
            EndGuardStartRatio = 1,
            MinimumBeatsBeforeEndForJumpDestination = 0
        });
        engine.Load(CreateAnalysis(6), CreateGraph(6, Edge(0, 4, 1.0), Edge(4, 1, 1.0)));

        engine.AdvanceToNextBeat();
        engine.Reset();

        engine.GetCurrentBeatIndex().Should().Be(0);
    }

    [Fact]
    public void Reset_Clears_Repeated_Jump_Blocks()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(
            CreateAnalysis(20),
            CreateGraph(
                20,
                Edge(0, 10, 1.0),
                Edge(0, 12, 0.95),
                Edge(10, 0, 1.0),
                Edge(12, 0, 1.0)));

        engine.AdvanceToNextBeat().Should().Be(10);

        engine.Reset();

        engine.AdvanceToNextBeat().Should().Be(10);
    }

    [Fact]
    public void AdvanceToNextBeat_Avoids_Repeated_Jump_When_Alternative_Exists()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(
            CreateAnalysis(20),
            CreateGraph(
                20,
                Edge(0, 10, 1.0),
                Edge(0, 12, 0.95),
                Edge(10, 0, 1.0),
                Edge(12, 0, 1.0)));

        engine.AdvanceToNextBeat().Should().Be(10);

        AdvanceUntilBeat(engine, 0);

        engine.AdvanceToNextBeat().Should().Be(12);
    }

    [Fact]
    public void AdvanceToNextBeat_Allows_Repeated_Jump_After_Avoidance_Passes()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(
            CreateAnalysis(20),
            CreateGraph(
                20,
                Edge(0, 10, 1.0),
                Edge(0, 12, 0.95),
                Edge(10, 0, 1.0),
                Edge(12, 0, 1.0)));

        engine.AdvanceToNextBeat().Should().Be(10);

        AdvanceUntilBeat(engine, 0);
        engine.AdvanceToNextBeat().Should().Be(12);

        AdvanceUntilBeat(engine, 0);
        engine.AdvanceToNextBeat().Should().Be(1);

        AdvanceUntilBeat(engine, 0);
        engine.AdvanceToNextBeat().Should().Be(10);
    }

    [Fact]
    public void AdvanceToNextBeat_Allows_Repeated_Jump_When_Needed_For_EndGuard_Escape()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 0,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0,
            EndGuardStartRatio = 0.80,
            MinimumBeatsBeforeEndForJumpDestination = 4,
            ForceJumpInEndGuard = true,
            RepeatedJumpAvoidancePasses = 2,
            AllowRepeatedJumpForTerminalEscape = true
        });
        engine.Load(
            CreateAnalysis(30),
            CreateGraph(
                30,
                Edge(24, 5, 1.0),
                Edge(5, 24, 1.0)));

        AdvanceUntilBeat(engine, 24);
        engine.AdvanceToNextBeat().Should().Be(5);

        AdvanceUntilBeat(engine, 24);
        engine.AdvanceToNextBeat().Should().Be(5);
    }

    [Fact]
    public void AdvanceToNextBeat_Does_Not_Jump_Before_FirstPassGate()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0.75
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(10, 60, 1.0)));

        AdvanceToBeat(engine, 10);

        engine.AdvanceToNextBeat().Should().Be(11);
    }

    [Fact]
    public void AdvanceToNextBeat_CanJump_After_FirstPassGate()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0.75
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(75, 30, 1.0)));

        AdvanceToBeat(engine, 75);

        engine.AdvanceToNextBeat().Should().Be(30);
    }

    [Fact]
    public void AdvanceToNextBeat_Blocks_TerminalTrap()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0.75,
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 12
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(75, 94, 1.0)));

        AdvanceToBeat(engine, 75);

        engine.AdvanceToNextBeat().Should().Be(76);
    }

    [Fact]
    public void AdvanceToNextBeat_Allows_TerminalDestination_WithEscape()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0.75,
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 12,
            TerminalEscapeLookaheadBeats = 8
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(75, 94, 1.0), Edge(96, 20, 1.0), Edge(60, 20, 1.0)));

        AdvanceToBeat(engine, 75);

        engine.AdvanceToNextBeat().Should().Be(94);
    }

    [Fact]
    public void AdvanceToNextBeat_Forces_SafeExit_In_EndGuard()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 0,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0.75,
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 11,
            ForceJumpInEndGuard = true
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(88, 30, 1.0)));

        AdvanceToBeat(engine, 88);

        engine.AdvanceToNextBeat().Should().Be(30);
    }

    [Fact]
    public void AdvanceToNextBeat_ChoosesSafeAlternative_WhenIntermediateBranchIsUnsafe()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0.75,
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 12
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(75, 80, 1.0), Edge(75, 40, 0.9)));

        AdvanceToBeat(engine, 75);

        engine.AdvanceToNextBeat().Should().Be(40);
    }

    [Fact]
    public void AdvanceToNextBeat_Forces_LastSafeExit_BeforeTerminal()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 0,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0,
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 11,
            ForceJumpInEndGuard = true
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(84, 30, 1.0)));

        AdvanceToBeat(engine, 84);

        engine.AdvanceToNextBeat().Should().Be(30);
    }

    [Fact]
    public void AdvanceToNextBeat_DoesNotForce_WhenFutureSafeExitExists()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 0,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0,
            EndGuardStartRatio = 0.88,
            MinimumBeatsBeforeEndForJumpDestination = 11,
            ForceJumpInEndGuard = true
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(80, 30, 1.0), Edge(84, 20, 1.0)));

        AdvanceToBeat(engine, 80);

        engine.AdvanceToNextBeat().Should().Be(81);
    }

    [Fact]
    public void ReloadGraph_Keeps_CurrentBeat()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(100), CreateGraph(100));

        AdvanceToBeat(engine, 50);
        engine.ReloadGraph(CreateGraph(100, Edge(50, 20, 1.0), Edge(20, 10, 1.0)));

        engine.GetCurrentBeatIndex().Should().Be(50);
    }

    [Fact]
    public void ReloadGraph_Uses_NewEdges_On_Next_Decision()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(100), CreateGraph(100));

        engine.ReloadGraph(CreateGraph(100, Edge(0, 20, 1.0), Edge(20, 10, 1.0)));

        engine.AdvanceToNextBeat().Should().Be(20);
    }

    [Fact]
    public void UpdateOptions_Applies_New_JumpProbability()
    {
        var engine = CreateEngine(new JukeboxEngineOptions
        {
            JumpProbability = 0,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0,
            EndGuardStartRatio = 1,
            MinimumBeatsBeforeEndForJumpDestination = 0
        });
        engine.Load(CreateAnalysis(100), CreateGraph(100, Edge(0, 20, 1.0), Edge(20, 10, 1.0)));

        engine.PeekNextBeatIndex().Should().Be(1);

        engine.UpdateOptions(CreateImmediateJumpOptions());

        engine.AdvanceToNextBeat().Should().Be(20);
    }

    [Fact]
    public void ReloadGraph_Rejects_Different_BeatCount()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(100), CreateGraph(100));

        FluentActions.Invoking(() => engine.ReloadGraph(CreateGraph(80)))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void SeekToBeat_ChangesCurrentBeat()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(100), CreateGraph(100));

        engine.SeekToBeat(42);

        engine.GetCurrentBeatIndex().Should().Be(42);
    }

    [Fact]
    public void SeekToBeat_ClampsInvalidBeat()
    {
        var engine = CreateEngine(CreateImmediateJumpOptions());
        engine.Load(CreateAnalysis(100), CreateGraph(100));

        engine.SeekToBeat(200);
        engine.GetCurrentBeatIndex().Should().Be(99);

        engine.SeekToBeat(-20);
        engine.GetCurrentBeatIndex().Should().Be(0);
    }

    private static GraphTraversalJukeboxEngine CreateEngine(JukeboxEngineOptions options)
    {
        return new GraphTraversalJukeboxEngine(
            options,
            NullLogger<GraphTraversalJukeboxEngine>.Instance,
            new Random(1234));
    }

    private static JukeboxEngineOptions CreateImmediateJumpOptions()
    {
        return new JukeboxEngineOptions
        {
            JumpProbability = 1,
            MinBeatsBeforeFirstJump = 0,
            JumpCooldown = 0,
            FirstPassLinearPlaybackRatio = 0,
            EndGuardStartRatio = 1,
            MinimumBeatsBeforeEndForJumpDestination = 0
        };
    }

    private static void AdvanceToBeat(GraphTraversalJukeboxEngine engine, int beatIndex)
    {
        while (engine.GetCurrentBeatIndex() < beatIndex)
        {
            engine.AdvanceToNextBeat();
        }
    }

    private static void AdvanceUntilBeat(GraphTraversalJukeboxEngine engine, int targetBeat, int maxSteps = 200)
    {
        for (var i = 0; i < maxSteps; i++)
        {
            if (engine.GetCurrentBeatIndex() == targetBeat)
            {
                return;
            }

            engine.AdvanceToNextBeat();
        }

        throw new InvalidOperationException($"Engine did not reach beat {targetBeat} within {maxSteps} steps.");
    }

    private static TrackAnalysis CreateAnalysis(int beatCount)
    {
        var beats = Enumerable.Range(0, beatCount)
            .Select(index => new Beat
            {
                Index = index,
                Start = index * 0.5,
                Duration = 0.5,
                Confidence = 1.0,
                Timbre = [1f, 0f],
                Pitches = [0f, 1f],
                Loudness = [0f, 0f, 0f],
                BarPosition = [0f, 1f]
            })
            .ToArray();

        return new TrackAnalysis
        {
            Metadata = new TrackMetadata
            {
                FileHash = "hash",
                FilePath = "test.wav",
                DurationSeconds = beatCount * 0.5,
                SampleRate = 22_050,
                Tempo = 120,
                TimeSignature = 4,
                SchemaVersion = TrackAnalysis.CurrentSchemaVersion
            },
            Segments = [],
            Beats = beats,
            Bars = [],
            Tatums = [],
            Sections = []
        };
    }

    private static JukeboxGraph CreateGraph(int beatCount, params JukeboxEdge[] edges)
    {
        var nodes = Enumerable.Range(0, beatCount)
            .Select(index => new JukeboxNode
            {
                BeatIndex = index,
                Start = index * 0.5,
                Duration = 0.5
            })
            .ToArray();

        return new JukeboxGraph
        {
            Nodes = nodes,
            JumpEdges = edges
                .GroupBy(edge => edge.FromBeat)
                .ToDictionary(group => group.Key, group => group.ToList()),
            SimilarityThreshold = 0.85,
            LookaheadDepth = 3
        };
    }

    private static JukeboxEdge Edge(int from, int to, double similarity)
    {
        return new JukeboxEdge
        {
            FromBeat = from,
            ToBeat = to,
            Similarity = similarity
        };
    }
}
