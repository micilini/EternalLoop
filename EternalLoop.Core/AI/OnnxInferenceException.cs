namespace EternalLoop.Core.AI;

public sealed class OnnxInferenceException : Exception
{
    public OnnxInferenceException(string message)
        : base(message)
    {
    }

    public OnnxInferenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
