namespace EternalLoop.AnalysisEngine.Core.Progress;

public enum AnalysisStage
{
    LoadingAudio,
    ExtractingFeatures,
    TrackingBeats,
    BuildingAnalysis,
    Validating,
    Done
}
