using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Console;
using SS14.Shared.IoC;
using Con = System.Console;

namespace SS14.Server.Console
{
    public class SystemConsoleManager : ISystemConsoleManager, IPostInjectInit, IDisposable
    {
        [Dependency]
        private readonly IConsoleShell _conShell;
        
        private readonly Dictionary<int, string> commandHistory = new Dictionary<int, string>();
        private string currentBuffer = "";
        private int historyIndex;
        private int internalCursor;
        private List<string> tabCompleteList = new List<string>();
        private int tabCompleteIndex;
        private ConsoleKey lastKeyPressed = ConsoleKey.NoName;

        public void Dispose()
        {
            Con.CancelKeyPress -= CancelKeyHandler;
        }

        public void PostInject()
        {
            Con.CancelKeyPress += CancelKeyHandler;
        }
        
        public void Update()
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
                            ExecuteCommand(currentBuffer);
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

        private void ExecuteCommand(string commandLine)
        {
            _conShell.ExecuteCommand(commandLine);
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

        public void ClearCurrentLine()
        {
            int currentLineCursor = Con.CursorTop;
            Con.SetCursorPosition(0, Con.CursorTop);
            for (int i = 0; i < Con.WindowWidth; i++)
                Con.Write(" ");
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
                tabCompleteList = _conShell.AvailableCommands.Keys.Where(key => key.StartsWith(currentBuffer)).ToList();
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

        private static void CancelKeyHandler(object sender, ConsoleCancelEventArgs args)
        {
            // Handle process exiting ourself.
            args.Cancel = true;
            IoCManager.Resolve<IBaseServer>().Shutdown(null);
        }
    }
}
