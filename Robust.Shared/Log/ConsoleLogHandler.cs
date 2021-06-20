using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Timers;
using JetBrains.Annotations;
using Serilog.Events;

namespace Robust.Shared.Log
{

    /// <summary>
    ///     Log handler that prints to console.
    /// </summary>
    public sealed class ConsoleLogHandler : ILogHandler, IDisposable
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

        private readonly Stream _stream = new BufferedStream(System.Console.OpenStandardOutput(), 128 * 1024);

        private readonly StringBuilder _line = new(1024);

        private readonly Timer _timer = new(0.1);

        private readonly bool _isUtf16Out = System.Console.OutputEncoding.CodePage == Encoding.Unicode.CodePage;

        static ConsoleLogHandler()
        {
            WriteAnsiColors = !System.Console.IsOutputRedirected;

            if (WriteAnsiColors && OperatingSystem.IsWindows())
            {
                WriteAnsiColors = WindowsConsole.TryEnableVirtualTerminalProcessing();
            }
        }

        public ConsoleLogHandler()
        {
            _timer.Start();
            _timer.Elapsed += (sender, args) =>
            {
                lock (_stream)
                {
                    if (IsConsoleActive)
                    {
                        _stream.Flush();
                    }
                }
            };
        }

#if DEBUG
        [UsedImplicitly]
#endif
        public static void TryDetachFromConsoleWindow()
        {
            if (OperatingSystem.IsWindows())
            {
                WindowsConsole.TryDetachFromConsoleWindow();
            }
        }

        private bool IsConsoleActive => !OperatingSystem.IsWindows() || WindowsConsole.IsConsoleActive;

        public void Log(string sawmillName, LogEvent message)
        {
            var robustLevel = message.Level.ToRobust();
            lock (_stream)
            {
                _line
                    .Clear()
                    .Append(LogLevelToString(robustLevel))
                    .Append(sawmillName)
                    .Append(": ")
                    .AppendLine(message.RenderMessage());

                if (message.Exception != null)
                {
                    _line.AppendLine(message.Exception.ToString());
                }

                // ReSharper disable once SuggestVarOrType_Elsewhere
                if (!_isUtf16Out)
                {
                    Span<byte> buf = stackalloc byte[1024];
                    var totalChars = _line.Length;
                    foreach (var chunk in _line.GetChunks())
                    {
                        var chunkSize = chunk.Length;
                        var totalRead = 0;
                        var span = chunk.Span;
                        for (;;)
                        {
                            var finalChunk = totalRead + chunkSize >= totalChars;
                            Utf8.FromUtf16(span, buf, out var read, out var wrote, isFinalBlock: finalChunk);
                            _stream.Write(buf.Slice(0, wrote));
                            totalRead += read;
                            if (read >= chunkSize)
                            {
                                break;
                            }

                            span = span[read..];
                            chunkSize -= read;
                        }
                    }
                }
                else
                {
                    foreach (var chunk in _line.GetChunks())
                    {
                        _stream.Write(MemoryMarshal.AsBytes(chunk.Span));
                    }
                }

                // ReSharper disable once InvertIf
                if (robustLevel >= LogLevel.Error)
                {
                    if (IsConsoleActive)
                    {
                        _stream.Flush();
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
                    LogLevel.Verbose => LogBeforeLevel + AnsiFgGreen + LogMessage.LogNameDebug + LogAfterLevel,
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
                LogLevel.Verbose => "[" + LogMessage.LogNameVerbose + "] ",
                LogLevel.Debug => "[" + LogMessage.LogNameDebug + "] ",
                LogLevel.Info => "[" + LogMessage.LogNameInfo + "] ",
                LogLevel.Warning => "[" + LogMessage.LogNameWarning + "] ",
                LogLevel.Error => "[" + LogMessage.LogNameError + "] ",
                LogLevel.Fatal => "[" + LogMessage.LogNameFatal + "] ",
                _ => "[" + LogMessage.LogNameUnknown +"] "
            };
        }

        public void Dispose()
        {
            lock (_stream)
            {
                _stream.Dispose();
                _timer.Dispose();
            }
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
