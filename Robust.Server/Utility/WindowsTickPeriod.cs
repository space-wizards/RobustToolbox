using System;
using System.Runtime.InteropServices;

namespace Robust.Server.Utility
{
    internal static class WindowsTickPeriod
    {
        private const uint TIMERR_NOERROR = 0;
        // This is an actual error code my god.
        private const uint TIMERR_NOCANDO = 97;

        public static void TimeBeginPeriod(uint period)
        {
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException();

            var ret = timeBeginPeriod(period);
            if (ret != TIMERR_NOERROR)
                throw new InvalidOperationException($"timeBeginPeriod returned error: {ret}");
        }

        public static void TimeEndPeriod(uint period)
        {
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException();

            var ret = timeEndPeriod(period);
            if (ret != TIMERR_NOERROR)
                throw new InvalidOperationException($"timeEndPeriod returned error: {ret}");
        }

        [DllImport("Winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("Winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);
    }
}
