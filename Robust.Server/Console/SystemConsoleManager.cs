using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Con = System.Console;

namespace Robust.Server.Console
{
    internal sealed class SystemConsoleManager : ISystemConsoleManager, IPostInjectInit, IDisposable
    {
        [Dependency] private readonly IServerConsoleHost _conShell = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;

        private readonly Dictionary<int, string> commandHistory = new();
        private string currentBuffer = "";
        private int historyIndex;
        private int internalCursor;
        private List<string> tabCompleteList = new();
        private int tabCompleteIndex;
        private ConsoleKey lastKeyPressed = ConsoleKey.NoName;

        public void Dispose()
        {
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

        public void Update()
        {
            if (Con.IsInputRedirected)
            {
                return;
            }

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
                            if (currentBuffer.Length > 0)
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
            Con.Write(new string(' ', Con.WindowWidth-1));
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
                tabCompleteList = _conShell.RegisteredCommands.Keys.Where(key => key.StartsWith(currentBuffer)).ToList();
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
            // Handle process exiting ourselves.
            args.Cancel = true;

            _taskManager.RunOnMainThread(() => { _baseServer.Shutdown("CancelKey"); });
        }
    }
}
