using Robust.Shared.Interfaces.Log;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Robust.Shared.Maths;

namespace Robust.Shared.Log
{

    /// <summary>
    ///     Log handler that prints to console.
    /// </summary>
    public sealed class ConsoleLogHandler : ILogHandler
    {

        private readonly Stream _writer = new BufferedStream(System.Console.OpenStandardOutput(), 2 * 1024 * 1024);

        private readonly StringBuilder _line = new StringBuilder(4096);

        private readonly Timer _timer = new Timer(0.1);

        public ConsoleLogHandler()
        {
            _timer.Start();
            _timer.Elapsed += (sender, args) =>
            {
                lock (_writer)
                {
                    if (IsConsoleActive)
                    {
                        _writer.Flush();
                    }
                }
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsConsole.TryEnableVirtualTerminalProcessing();
            }
        }

        public static void TryDetachFromConsoleWindow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsConsole.TryDetachFromConsoleWindow();
            }
        }

        private bool IsConsoleActive => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && WindowsConsole.IsConsoleActive;

        public void Log(in LogMessage message)
        {
            var name = LogMessage.LogLevelToName(message.Level);
            var color = LogLevelToConsoleColor(message.Level);
            lock (_writer)
            {
                _line
                    .Clear()
                    .Append("\x1B[39m")
                    .Append("[")
                    .Append("\x1B[38;2;")
                    .Append(color.RByte)
                    .Append(';')
                    .Append(color.GByte)
                    .Append(';')
                    .Append(color.BByte)
                    .Append('m')
                    .Append(name)
                    .Append("\x1B[39m")
                    .Append("] ")
                    .Append(message.SawmillName)
                    .Append(": ")
                    .Append(message.Message)
                    .AppendLine();
                foreach (var chunk in _line.GetChunks())
                {
                    _writer.Write(MemoryMarshal.AsBytes(chunk.Span));
                }

                if (message.Level >= LogLevel.Error)
                {
                    if (IsConsoleActive)
                    {
                        _writer.Flush();
                    }
                }
            }
        }

        private static Color LogLevelToConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return Color.Navy;

                case LogLevel.Info:
                    return Color.Cyan;

                case LogLevel.Warning:
                    return Color.Yellow;

                case LogLevel.Error:
                    return Color.DarkRed;

                case LogLevel.Fatal:
                    return Color.Magenta;

                default:
                    return Color.White;
            }
        }

    }

    public static class WindowsConsole
    {

        public static bool TryEnableVirtualTerminalProcessing()
        {
            try
            {
                var stdHandle = NativeMethods.GetStdHandle(-11);
                NativeMethods.GetConsoleMode(stdHandle, out var mode);
                NativeMethods.SetConsoleMode(stdHandle, mode | 4);
                NativeMethods.GetConsoleMode(stdHandle, out mode);
                return (mode & 4) == 4;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        private static bool _freedConsole;

        public static bool IsConsoleActive => !_freedConsole;

        public static void TryDetachFromConsoleWindow()
        {
            if (NativeMethods.GetConsoleWindow() == default
                || Debugger.IsAttached
                || System.Console.IsOutputRedirected
                || System.Console.IsErrorRedirected
                || System.Console.IsInputRedirected)
            {
                return;
            }

            _freedConsole = NativeMethods.FreeConsole();
        }

        internal static class NativeMethods
        {

            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool GetConsoleMode(IntPtr handle, out int mode);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern IntPtr GetStdHandle(int handle);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool FreeConsole();

            [DllImport("kernel32", SetLastError = true)]
            internal static extern IntPtr GetConsoleWindow();

        }

    }

}
