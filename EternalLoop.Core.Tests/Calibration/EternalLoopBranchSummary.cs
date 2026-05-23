namespace EternalLoop.Core.Tests.Calibration;

public sealed record EternalLoopBranchSummary(
    string Preset,
    bool UseAi,
    double DurationSeconds,
    double Tempo,
    int BeatCount,
    int EdgeCount,
    int SourceCount,
    double SourceRatio,
    int BackwardEdgeCount,
    int ForwardEdgeCount,
    int LongBackwardEdgeCount,
    int MetricMatchedEdgeCount,
    string CsvPath,
    string SummaryPath);
