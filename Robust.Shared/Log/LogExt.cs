using System.Runtime.CompilerServices;
using Serilog.Events;

namespace Robust.Shared.Log
{
    public static class LogExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogLevel ToRobust(this LogEventLevel level)
        {
            return (LogLevel) level;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogEventLevel ToSerilog(this LogLevel level)
        {
            return (LogEventLevel) level;
        }
    }
}
