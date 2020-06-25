using Robust.Shared.Interfaces.Log;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace Robust.Shared.Log
{

    /// <summary>
    ///     Log handler that prints to console.
    /// </summary>
    public sealed class ConsoleLogHandler : ILogHandler
    {
        private static readonly bool WriteAnsiColors;

        // ReSharper disable UnusedMember.Local
        private const string AnsiCsi = "\x1B[";
        private const string AnsiFgDefault = AnsiCsi + "39m";
        private const string AnsiFgBlack = AnsiCsi + "30m";
        private const string AnsiFgRed = AnsiCsi + "31m";
        private const string AnsiFgBrightRed = AnsiCsi + "91m";
        private const string AnsiFgGreen = AnsiCsi + "32m";
        private const string AnsiFgBrightGreen = AnsiCsi + "92m";
        private const string AnsiFgYellow = AnsiCsi + "33m";
        private const string AnsiFgBrightYellow = AnsiCsi + "93m";
        private const string AnsiFgBlue = AnsiCsi + "34m";
        private const string AnsiFgBrightBlue = AnsiCsi + "94m";
        private const string AnsiFgMagenta = AnsiCsi + "35m";
        private const string AnsiFgBrightMagenta = AnsiCsi + "95m";
        private const string AnsiFgCyan = AnsiCsi + "36m";
        private const string AnsiFgBrightCyan = AnsiCsi + "96m";
        private const string AnsiFgWhite = AnsiCsi + "37m";
        private const string AnsiFgBrightWhite = AnsiCsi + "97m";
        // ReSharper restore UnusedMember.Local

        private const string LogBeforeLevel = AnsiFgDefault + "[";
        private const string LogAfterLevel = AnsiFgDefault + "] ";

        private readonly Stream _writer = new BufferedStream(System.Console.OpenStandardOutput(), 2 * 1024 * 1024);

        private readonly StreamWriter _textWriter;

        private readonly StringBuilder _line = new StringBuilder(4096);

        private readonly Timer _timer = new Timer(0.1);

        static ConsoleLogHandler()
        {
            WriteAnsiColors = !System.Console.IsOutputRedirected;

            if (WriteAnsiColors && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WriteAnsiColors = WindowsConsole.TryEnableVirtualTerminalProcessing();
            }
        }

        public ConsoleLogHandler()
        {
            _textWriter = new StreamWriter(_writer, System.Console.OutputEncoding);

            _timer.Start();
            _timer.Elapsed += (sender, args) =>
            {
                lock (_textWriter)
                {
                    if (IsConsoleActive)
                    {
                        _textWriter.Flush();
                    }
                }
            };
        }

        public static void TryDetachFromConsoleWindow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsConsole.TryDetachFromConsoleWindow();
            }
        }

        private bool IsConsoleActive => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || WindowsConsole.IsConsoleActive;

        public void Log(in LogMessage message)
        {
            lock (_textWriter)
            {
                _line
                    .Clear()
                    .Append(LogLevelToString(message.Level))
                    .Append(message.SawmillName)
                    .Append(": ")
                    .Append(message.Message)
                    .AppendLine();

                foreach (var chunk in _line.GetChunks())
                {
                    _textWriter.Write(chunk.Span);
                }

                if (message.Level >= LogLevel.Error)
                {
                    if (IsConsoleActive)
                    {
                        _textWriter.Flush();
                    }
                }
            }
        }

        private static string LogLevelToString(LogLevel level)
        {
            if (WriteAnsiColors)
            {
                return level switch
                {
                    LogLevel.Debug => LogBeforeLevel + AnsiFgBlue + LogMessage.LogNameDebug + LogAfterLevel,
                    LogLevel.Info => LogBeforeLevel + AnsiFgBrightCyan + LogMessage.LogNameInfo + LogAfterLevel,
                    LogLevel.Warning => LogBeforeLevel + AnsiFgBrightYellow + LogMessage.LogNameWarning + LogAfterLevel,
                    LogLevel.Error => LogBeforeLevel + AnsiFgBrightRed + LogMessage.LogNameError + LogAfterLevel,
                    LogLevel.Fatal => LogBeforeLevel + AnsiFgBrightMagenta + LogMessage.LogNameFatal + LogAfterLevel,
                    _ => LogBeforeLevel + AnsiFgWhite + LogMessage.LogNameUnknown + LogAfterLevel
                };
            }

            return level switch
            {
                LogLevel.Debug => "[" + LogMessage.LogNameDebug + "] ",
                LogLevel.Info => "[" + LogMessage.LogNameInfo + "] ",
                LogLevel.Warning => "[" + LogMessage.LogNameWarning + "] ",
                LogLevel.Error => "[" + LogMessage.LogNameError + "] ",
                LogLevel.Fatal => "[" + LogMessage.LogNameFatal + "] ",
                _ => "[" + LogMessage.LogNameUnknown +"] "
            };
        }
    }

    internal static class WindowsConsole
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
