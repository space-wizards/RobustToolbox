using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Unicode;
using System.Timers;
using JetBrains.Annotations;
using Serilog.Events;
using TerraFX.Interop.Windows;

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

        private bool _disposed;

        static ConsoleLogHandler()
        {
            WriteAnsiColors = !System.Console.IsOutputRedirected;

            if (WriteAnsiColors && OperatingSystem.IsWindows())
            {
                WriteAnsiColors = WindowsConsole.TryEnableVirtualTerminalProcessing();
            }

            // Set console output on Windows to UTF-8, because .NET doesn't do it built-in.
            // Otherwise we can't print anything that isn't just your default Windows code page.
            try
            {
                System.Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
                // If this doesn't work, RIP.
            }
        }

        public ConsoleLogHandler()
        {
            _timer.Start();
            _timer.Elapsed += (sender, args) =>
            {
                lock (_stream)
                {
                    if (IsConsoleActive && !_disposed)
                    {
                        _stream.Flush();
                    }
                }
            };
        }

        [UsedImplicitly]
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

                if (System.Console.OutputEncoding.CodePage == WindowsConsole.NativeMethods.CodePageUtf8)
                {
                    // Fast path: if we can output as UTF-8, do it.
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
                    // Fallback path: just let .NET handle it.
                    System.Console.Write(_line.ToString());
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

        internal static string LogLevelToString(LogLevel level)
        {
            if (WriteAnsiColors)
            {
                return level switch
                {
                    LogLevel.Verbose => LogBeforeLevel + AnsiFgGreen + LogMessage.LogNameVerbose + LogAfterLevel,
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
                _disposed = true;
                _timer.Dispose();
                _stream.Dispose();
            }
        }
    }

    internal static class WindowsConsole
    {

        public static unsafe bool TryEnableVirtualTerminalProcessing()
        {
            try
            {
                var stdHandle = Windows.GetStdHandle(unchecked((uint)-11));
                uint mode;
                Windows.GetConsoleMode(stdHandle, &mode);
                Windows.SetConsoleMode(stdHandle, mode | 4);
                Windows.GetConsoleMode(stdHandle, &mode);
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
            if (Windows.GetConsoleWindow() == default
                || Debugger.IsAttached
                || System.Console.IsOutputRedirected
                || System.Console.IsErrorRedirected
                || System.Console.IsInputRedirected)
            {
                return;
            }

            _freedConsole = Windows.FreeConsole();
        }

        internal static class NativeMethods
        {
            public const int CodePageUtf8 = 65001;
        }

    }

}
