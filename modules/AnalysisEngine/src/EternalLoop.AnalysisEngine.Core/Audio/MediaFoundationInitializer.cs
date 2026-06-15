using NAudio.MediaFoundation;
using System.Threading;

namespace EternalLoop.AnalysisEngine.Core.Audio;

internal static class MediaFoundationInitializer
{
    private static int _started;

    public static void EnsureStarted()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        MediaFoundationApi.Startup();
    }
}
