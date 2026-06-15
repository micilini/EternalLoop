using System.Text.Json.Nodes;
using EternalLoop.BranchAnalysis.Core.Models;

namespace EternalLoop.BranchAnalysis.Tests.Fixtures;

public static class AnalysisFixtureFactory
{
    public static string WriteValidAnalysisFile(string analysisRoot, string name)
    {
        string directory = Path.Combine(analysisRoot, name);
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, "eternalloop-analysis.json");
        File.WriteAllText(filePath, CreateValidAnalysisNode().ToJsonString());
        return filePath;
    }

    public static string WriteParityAnalysisFile(string analysisRoot, string name)
    {
        string directory = Path.Combine(analysisRoot, name);
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, "eternalloop-analysis.json");
        File.WriteAllText(filePath, CreateParityAnalysisNode().ToJsonString());
        return filePath;
    }

    public static string WriteInvalidAnalysisFile(string analysisRoot, string name)
    {
        string directory = Path.Combine(analysisRoot, name);
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, "eternalloop-analysis.json");
        File.WriteAllText(filePath, new JsonObject
        {
            ["info"] = new JsonObject(),
            ["analysis"] = new JsonObject
            {
                ["sections"] = new JsonArray(),
                ["bars"] = new JsonArray(),
                ["beats"] = new JsonArray(),
                ["tatums"] = new JsonArray(),
                ["segments"] = new JsonArray()
            },
            ["audio_summary"] = new JsonObject()
        }.ToJsonString());
        return filePath;
    }

    public static string CreateValidAnalysisJsonText()
    {
        return CreateValidAnalysisNode().ToJsonString();
    }

    public static JsonNode CreateValidAnalysisNode()
    {
        return new JsonObject
        {
            ["info"] = new JsonObject
            {
                ["service"] = "LOCAL",
                ["id"] = "fixture-track",
                ["title"] = "Fixture Track",
                ["name"] = "Fixture Track",
                ["artist"] = "Local",
                ["url"] = "local://fixture-track.mp3",
                ["duration"] = 8000
            },
            ["analysis"] = new JsonObject
            {
                ["sections"] = new JsonArray
                {
                    CreateQuantum(0, 8)
                },
                ["bars"] = new JsonArray
                {
                    CreateQuantum(0, 4),
                    CreateQuantum(4, 4)
                },
                ["beats"] = CreateQuanta(8, 1),
                ["tatums"] = CreateQuanta(8, 1),
                ["segments"] = new JsonArray
                {
                    CreateSegment(0.1, 0.2, 1, 1),
                    CreateSegment(1.1, 0.2, 5, 2),
                    CreateSegment(2.1, 0.2, 9, 3),
                    CreateSegment(3.1, 0.2, 13, 4),
                    CreateSegment(4.1, 0.2, 1.1, 1),
                    CreateSegment(5.1, 0.2, 5.1, 2),
                    CreateSegment(6.1, 0.2, 9.1, 3),
                    CreateSegment(7.1, 0.2, 13.1, 4)
                }
            },
            ["audio_summary"] = new JsonObject
            {
                ["duration"] = 8
            }
        };
    }

    public static JsonNode CreateParityAnalysisNode()
    {
        return new JsonObject
        {
            ["info"] = new JsonObject
            {
                ["service"] = "LOCAL",
                ["id"] = "parity-song-a",
                ["title"] = "Parity Song A",
                ["name"] = "Parity Song A",
                ["artist"] = "Parity Artist",
                ["url"] = "local://parity-song-a.mp3",
                ["duration"] = 16000
            },
            ["analysis"] = new JsonObject
            {
                ["sections"] = new JsonArray
                {
                    CreateQuantum(0, 8),
                    CreateQuantum(8, 8)
                },
                ["bars"] = new JsonArray
                {
                    CreateQuantum(0, 4),
                    CreateQuantum(4, 4),
                    CreateQuantum(8, 4),
                    CreateQuantum(12, 4)
                },
                ["beats"] = CreateQuanta(16, 1),
                ["tatums"] = CreateQuanta(32, 0.5),
                ["segments"] = CreateParitySegments()
            },
            ["audio_summary"] = new JsonObject
            {
                ["duration"] = 16
            }
        };
    }

    public static TrackAnalysisDocument CreatePreprocessingAnalysisDocument()
    {
        return new TrackAnalysisDocument
        {
            Info = new TrackInfo
            {
                Id = "fixture-track",
                Title = "Fixture Track",
                Artist = "Local"
            },
            AudioSummary = new AudioSummary
            {
                Duration = 8
            },
            Analysis = new AnalysisData
            {
                Sections = [CreateTimeQuantum(0, 8)],
                Bars =
                [
                    CreateTimeQuantum(0, 4),
                    CreateTimeQuantum(4, 4)
                ],
                Beats = CreateTimeQuanta(8, 1),
                Tatums = CreateTimeQuanta(16, 0.5),
                Segments =
                [
                    CreateSegmentQuantum(0.1, 0.25, 0.9, [1, 0, 0], [1, 0]),
                    CreateSegmentQuantum(0.45, 0.25, 0.9, [2, 0, 0], [0, 1]),
                    CreateSegmentQuantum(0.8, 0.25, 0.9, [3, 0, 0], [0.5, 0.5]),
                    CreateSegmentQuantum(1.1, 0.25, 0.9, [4, 0, 0], [1, 0]),
                    CreateSegmentQuantum(2.1, 0.25, 0.9, [5, 0, 0], [1, 0]),
                    CreateSegmentQuantum(3.1, 0.25, 0.9, [6, 0, 0], [1, 0]),
                    CreateSegmentQuantum(4.1, 0.25, 0.9, [7, 0, 0], [1, 0]),
                    CreateSegmentQuantum(5.1, 0.25, 0.9, [8, 0, 0], [1, 0]),
                    CreateSegmentQuantum(6.1, 0.25, 0.9, [9, 0, 0], [1, 0]),
                    CreateSegmentQuantum(7.1, 0.25, 0.9, [10, 0, 0], [1, 0])
                ]
            }
        };
    }

    private static JsonArray CreateQuanta(int count, double duration)
    {
        JsonArray values = [];

        for (int index = 0; index < count; index++)
        {
            values.Add(CreateQuantum(index * duration, duration));
        }

        return values;
    }

    private static JsonObject CreateQuantum(double start, double duration)
    {
        return new JsonObject
        {
            ["start"] = start,
            ["duration"] = duration,
            ["confidence"] = 1
        };
    }

    private static JsonObject CreateSegment(double start, double duration, double timbreSeed, double pitchSeed)
    {
        return new JsonObject
        {
            ["start"] = start,
            ["duration"] = duration,
            ["confidence"] = 1,
            ["loudness_start"] = 0,
            ["loudness_max"] = 0,
            ["loudness_max_time"] = 0,
            ["timbre"] = CreateVector(26, timbreSeed),
            ["pitches"] = CreateVector(12, pitchSeed)
        };
    }

    private static JsonArray CreateParitySegments()
    {
        JsonArray segments = [];
        double[] timbrePattern = [1, 4, 8, 12, 1.1, 4.1, 8.1, 12.1];
        double[] pitchPattern = [1, 2, 3, 4, 1, 2, 3, 4];

        for (int index = 0; index < 24; index++)
        {
            int patternIndex = index % timbrePattern.Length;
            double start = 0.1 + index * 0.65;
            segments.Add(new JsonObject
            {
                ["start"] = start,
                ["duration"] = 0.32,
                ["confidence"] = 0.75 + patternIndex / 100.0,
                ["loudness_start"] = -12 + patternIndex / 10.0,
                ["loudness_max"] = -4 + patternIndex / 10.0,
                ["loudness_max_time"] = 0.05,
                ["timbre"] = CreateVector(26, timbrePattern[patternIndex]),
                ["pitches"] = CreateVector(12, pitchPattern[patternIndex])
            });
        }

        return segments;
    }

    private static JsonArray CreateVector(int length, double seed)
    {
        JsonArray values = [];

        for (int index = 0; index < length; index++)
        {
            values.Add(seed + index / 100.0);
        }

        return values;
    }

    private static List<TimeQuantum> CreateTimeQuanta(int count, double duration)
    {
        List<TimeQuantum> values = [];

        for (int index = 0; index < count; index++)
        {
            values.Add(CreateTimeQuantum(index * duration, duration));
        }

        return values;
    }

    private static TimeQuantum CreateTimeQuantum(double start, double duration)
    {
        return new TimeQuantum
        {
            Start = start,
            Duration = duration,
            Confidence = 1
        };
    }

    private static SegmentQuantum CreateSegmentQuantum(
        double start,
        double duration,
        double confidence,
        List<double> timbre,
        List<double> pitches)
    {
        return new SegmentQuantum
        {
            Start = start,
            Duration = duration,
            Confidence = confidence,
            LoudnessStart = 0,
            LoudnessMax = 0,
            LoudnessMaxTime = 0,
            Timbre = timbre,
            Pitches = pitches
        };
    }
}
