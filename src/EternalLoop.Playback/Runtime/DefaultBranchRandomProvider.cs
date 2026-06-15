namespace EternalLoop.Playback.Runtime;

public sealed class DefaultBranchRandomProvider : IBranchRandomProvider
{
    public double NextDouble()
    {
        return Random.Shared.NextDouble();
    }
}
