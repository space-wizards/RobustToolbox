using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Con = System.Console;

namespace Robust.Server.Console
{
    internal sealed class SystemConsoleManager : ISystemConsoleManager, IPostInjectInit, IDisposable
    {
        [Dependency] private readonly IServerConsoleHost _conShell = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IGameTiming _time = default!;

        //
        // Command entry stuff.
        //

        private readonly Dictionary<int, string> commandHistory = new();
        private string currentBuffer = "";
        private int historyIndex;
        private int internalCursor;
        private List<string> tabCompleteList = new();
        private int tabCompleteIndex;
        private ConsoleKey lastKeyPressed = ConsoleKey.NoName;

        //
        // Title update stuff.
        //

        // This is ridiculously expensive to fetch for some reason.
        // I'm gonna just assume that this can't change during the lifetime of the process. I hope.
        // I want this ridiculous 0.1% CPU usage off my profiler.
        private readonly bool _userInteractive = Environment.UserInteractive && !System.Console.IsInputRedirected;

        private TimeSpan _lastTitleUpdate;
        private long _lastReceivedBytes;
        private long _lastSentBytes;

        private bool _hasCancelled;

        public void Dispose()
        {
            if (_rdLine != null)
            {
                _rdLine.Value.cts.Cancel();
                _rdLine.Value.cts.Dispose();

                try
                {
                    _rdLine.Value.task.Dispose();
                }
                catch
                {
                    // Don't care LOL
                }
            }

            if (Environment.UserInteractive)
            {
                Con.CancelKeyPress -= CancelKeyHandler;
            }
        }

        public void PostInject()
        {
            if (Environment.UserInteractive)
            {
                Con.CancelKeyPress += CancelKeyHandler;
            }
        }

        public void UpdateTick()
        {
            UpdateTitle();
        }

        /// <summary>
        ///     Updates the console window title with performance statistics.
        /// </summary>
        private void UpdateTitle()
        {
            if (!_userInteractive)
                return;

            // every 1 second update stats in the console window title
            if ((_time.RealTime - _lastTitleUpdate).TotalSeconds < 1.0)
                return;

            var netStats = UpdateBps();
            var privateSize = Process.GetCurrentProcess().GetPrivateMemorySize64NotSlowHolyFuckingShitMicrosoft();

            System.Console.Title = string.Format("FPS: {0:N2} SD: {1:N2}ms | Net: ({2}) | Memory: {3:N0} KiB",
                Math.Round(_time.FramesPerSecondAvg, 2),
                _time.RealFrameTimeStdDev.TotalMilliseconds,
                netStats,
                privateSize >> 10);
            _lastTitleUpdate = _time.RealTime;
        }

        private string UpdateBps()
        {
            var stats = _netManager.Statistics;

            var bps =
                $"Send: {(stats.SentBytes - _lastSentBytes) >> 10:N0} KiB/s, Recv: {(stats.ReceivedBytes - _lastReceivedBytes) >> 10:N0} KiB/s";

            _lastSentBytes = stats.SentBytes;
            _lastReceivedBytes = stats.ReceivedBytes;

            return bps;
        }

        private (Task task, CancellationTokenSource cts, Channel<string> chan)? _rdLine = null;

        public void UpdateInput()
        {
            if (_userInteractive)
            {
                HandleKeyboard();
                return;
            }

            if (_rdLine.HasValue) // Already running, check the channel.
            {
                if (_rdLine.Value.chan.Reader.TryRead(out var cmd))
                    _conShell.ExecuteCommand(cmd);

                return;
            }

            // Set up the new thread & thread accessories
            var rlc = new CancellationTokenSource();
            var chan = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity: 32)
                {
                    FullMode=BoundedChannelFullMode.Wait,
                    SingleReader=true,
                }
            );

            _rdLine = (
                task: Task.Run(
                    async () =>
                    {
                        while (!rlc.IsCancellationRequested)
                        {
                            var str = await Con.In
                                .ReadLineAsync()
                                .WaitAsync(TimeSpan.FromSeconds(2.0), rlc.Token);

                            await chan.Writer.WriteAsync(str!, rlc.Token);
                        }
                    },
                    rlc.Token
                ),
                cts: rlc,
                chan: chan
            );
        }

        public void HandleKeyboard()
        {
            // Process keyboard input
            while (Con.KeyAvailable)
            {
                ConsoleKeyInfo key = Con.ReadKey(true);
                Con.SetCursorPosition(0, Con.CursorTop);
                if (!Char.IsControl(key.KeyChar))
                {
                    currentBuffer = currentBuffer.Insert(internalCursor++, key.KeyChar.ToString());
                    DrawCommandLine();
                }
                else
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            if (currentBuffer.Length == 0)
                                break;
                            Con.WriteLine("> " + currentBuffer);
                            commandHistory.Add(commandHistory.Count, currentBuffer);
                            historyIndex = commandHistory.Count;
                            _conShell.ExecuteCommand(currentBuffer);
                            currentBuffer = "";
                            internalCursor = 0;
                            break;

                        case ConsoleKey.Backspace:
                            if (currentBuffer.Length > 0 && internalCursor > 0)
                            {
                                currentBuffer = currentBuffer.Remove(internalCursor - 1, 1);
                                internalCursor--;
                            }

                            break;

                        case ConsoleKey.Delete:
                            if (currentBuffer.Length > 0 && internalCursor < currentBuffer.Length)
                            {
                                currentBuffer = currentBuffer.Remove(internalCursor, 1);
                            }

                            break;

                        case ConsoleKey.UpArrow:
                            if (historyIndex > 0)
                                historyIndex--;
                            if (commandHistory.ContainsKey(historyIndex))
                                currentBuffer = commandHistory[historyIndex];
                            else
                                currentBuffer = "";
                            internalCursor = currentBuffer.Length;
                            break;

                        case ConsoleKey.DownArrow:
                            if (historyIndex < commandHistory.Count)
                                historyIndex++;
                            if (commandHistory.ContainsKey(historyIndex))
                                currentBuffer = commandHistory[historyIndex];
                            else
                                currentBuffer = "";
                            internalCursor = currentBuffer.Length;
                            break;

                        case ConsoleKey.Escape:
                            historyIndex = commandHistory.Count;
                            currentBuffer = "";
                            internalCursor = 0;
                            break;

                        case ConsoleKey.LeftArrow:
                            if (internalCursor > 0)
                                internalCursor--;
                            break;

                        case ConsoleKey.RightArrow:
                            if (internalCursor < currentBuffer.Length)
                                internalCursor++;
                            break;

                        case ConsoleKey.Tab:
                            if (lastKeyPressed != ConsoleKey.Tab)
                            {
                                tabCompleteList.Clear();
                            }

                            string tabCompleteResult = TabComplete();
                            if (tabCompleteResult != String.Empty)
                            {
                                currentBuffer = tabCompleteResult;
                                internalCursor = currentBuffer.Length;
                            }

                            break;
                    }

                    lastKeyPressed = key.Key;
                    DrawCommandLine();
                }
            }
        }

        public void Print(string text)
        {
            Con.Write(text);
        }

        public void DrawCommandLine()
        {
            ClearCurrentLine();
            Con.SetCursorPosition(0, Con.CursorTop);
            Con.Write("> " + currentBuffer);
            Con.SetCursorPosition(internalCursor + 2, Con.CursorTop); //+2 is for the "> " at the beginning of the line
        }

        private static void ClearCurrentLine()
        {
            var currentLineCursor = Con.CursorTop;
            Con.SetCursorPosition(0, Con.CursorTop);
            Con.Write(new string(' ', Con.WindowWidth - 1));
            Con.SetCursorPosition(0, currentLineCursor);
        }

        private string TabComplete()
        {
            if (currentBuffer.Trim() == String.Empty)
            {
                return String.Empty;
            }

            if (tabCompleteList.Count == 0)
            {
                tabCompleteList = _conShell.AvailableCommands.Keys.Where(key => key.StartsWith(currentBuffer))
                    .ToList();
                if (tabCompleteList.Count == 0)
                {
                    return String.Empty;
                }
            }

            if (tabCompleteIndex + 1 > tabCompleteList.Count)
            {
                tabCompleteIndex = 0;
            }

            string result = tabCompleteList[tabCompleteIndex];
            tabCompleteIndex++;
            return result;
        }

        private void CancelKeyHandler(object? sender, ConsoleCancelEventArgs args)
        {
            if (_hasCancelled)
            {
                Con.WriteLine("Double CancelKey, terminating process.");
                return;
            }

            // Handle process exiting ourselves.
            args.Cancel = true;
            _hasCancelled = true;

            _taskManager.RunOnMainThread(() => { _baseServer.Shutdown("CancelKey"); });
        }
    }
}
