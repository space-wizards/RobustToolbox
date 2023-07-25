using System;
using TerraFX.Interop.Windows;

namespace Robust.Server.Utility
{
    internal static class WindowsTickPeriod
    {
        public static void TimeBeginPeriod(uint period)
        {
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException();

            var ret = Windows.timeBeginPeriod(period);
            if (ret != Windows.TIMERR_NOERROR)
                throw new InvalidOperationException($"timeBeginPeriod returned error: {ret}");
        }

        public static void TimeEndPeriod(uint period)
        {
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException();

            var ret = Windows.timeBeginPeriod(period);
            if (ret != Windows.TIMERR_NOERROR)
                throw new InvalidOperationException($"timeEndPeriod returned error: {ret}");
        }
    }
}
