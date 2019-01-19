using System.Diagnostics.Contracts;
using System.Threading;

namespace SS14.Client.Utility
{
    public static class ThreadUtility
    {
        public static Thread MainThread { get; internal set; }

        [Pure]
        public static bool IsOnMainThread()
        {
            return Thread.CurrentThread == MainThread;
        }
    }
}
