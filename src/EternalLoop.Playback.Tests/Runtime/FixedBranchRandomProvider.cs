using EternalLoop.Playback.Runtime;

namespace EternalLoop.Playback.Tests.Runtime;

internal sealed class FixedBranchRandomProvider(double value) : IBranchRandomProvider
{
    public double NextDouble()
    {
        return value;
    }
}
