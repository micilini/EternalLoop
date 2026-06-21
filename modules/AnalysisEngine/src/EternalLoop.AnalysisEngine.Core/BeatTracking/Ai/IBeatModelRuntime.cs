namespace EternalLoop.AnalysisEngine.Core.BeatTracking.Ai;

public interface IBeatModelRuntime : IDisposable
{
    string ModelPath { get; }

    IReadOnlyList<string> InputNames { get; }

    IReadOnlyList<string> OutputNames { get; }

    BeatThisInferenceResult Run(
        BeatThisInputTensor inputTensor,
        BeatThisModelMetadata metadata);
}
