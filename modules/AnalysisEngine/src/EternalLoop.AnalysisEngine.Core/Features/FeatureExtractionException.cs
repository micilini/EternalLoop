namespace EternalLoop.AnalysisEngine.Core.Features;

public sealed class FeatureExtractionException : Exception
{
    public FeatureExtractionException(string message)
        : base(message)
    {
    }

    public FeatureExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
