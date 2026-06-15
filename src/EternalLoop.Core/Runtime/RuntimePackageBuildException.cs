namespace EternalLoop.Core.Runtime;

public sealed class RuntimePackageBuildException : Exception
{
    public RuntimePackageBuildException(string message)
        : base(message)
    {
    }

    public RuntimePackageBuildException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
