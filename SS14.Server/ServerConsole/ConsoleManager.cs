using SS14.Server.Interfaces.ServerConsole;
using SS14.Server.Interfaces;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using Con = System.Console;

namespace SS14.Server.ServerConsole
{
    public class ConsoleManager : IConsoleManager, IPostInjectInit, IDisposable
    {
        [Dependency]
        private readonly IConfigurationManager configurationManager;
        private Dictionary<string, IConsoleCommand> availableCommands = new Dictionary<string, IConsoleCommand>();
        private readonly Dictionary<int, string> commandHistory = new Dictionary<int, string>();
        private string currentBuffer = "";
        private int historyIndex;
        private int internalCursor;
        private List<string> tabCompleteList = new List<string>();
        private int tabCompleteIndex;
        private ConsoleKey lastKeyPressed = ConsoleKey.NoName;
        public IReadOnlyDictionary<string, IConsoleCommand> AvailableCommands => availableCommands;

        public void Dispose()
        {
            Con.CancelKeyPress -= CancelKeyHandler;
        }

        public void PostInject()
        {
            configurationManager.RegisterCVar("console.width", 120, CVarFlags.ARCHIVE);
            configurationManager.RegisterCVar("console.height", 60, CVarFlags.ARCHIVE);

            Con.CancelKeyPress += CancelKeyHandler;
        }

        #region IConsoleManager Members

        public void Initialize()
        {
            var consoleWidth = configurationManager.GetCVar<int>("console.width");
            var consoleHeight = configurationManager.GetCVar<int>("console.height");

            try
            {
                Con.SetWindowSize(consoleWidth, consoleHeight);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Con.WriteLine("Resizing Failure:");
                Con.WriteLine(e.Message);
            }

            var manager = IoCManager.Resolve<IReflectionManager>();
            foreach (var type in manager.GetAllChildren<IConsoleCommand>())
            {
                var instance = Activator.CreateInstance(type) as IConsoleCommand;
                RegisterCommand(instance);
            }
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

        #endregion IConsoleManager Members

        private void ExecuteCommand(string commandLine)
        {
            List<string> args = new List<string>(commandLine.Split(' '));
            if (args.Count == 0)
            {
                return;
            }
            string cmd = args[0].ToLower();

            try
            {
                IConsoleCommand command = AvailableCommands[cmd];
                args.RemoveAt(0);
                command.Execute(args.ToArray());
            }
            catch (KeyNotFoundException)
            {
                Con.ForegroundColor = ConsoleColor.Red;
                Con.WriteLine("Unknown command: '{0}'", cmd);
                Con.ResetColor();
            }
            catch (Exception e)
            {
                Con.ForegroundColor = ConsoleColor.Red;
                Con.WriteLine("There was an error while executing the command:\n{0}", e);
                Con.ResetColor();
            }
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
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private void RegisterCommand(IConsoleCommand commandObj)
        {
            if (!availableCommands.ContainsKey(commandObj.Command.ToLower()))
                availableCommands.Add(commandObj.Command.ToLower(), commandObj);
        }

        private string TabComplete()
        {
            if (currentBuffer.Trim() == String.Empty)
            {
                return String.Empty;
            }

            if (tabCompleteList.Count == 0)
            {
                tabCompleteList = availableCommands.Keys.Where(key => key.StartsWith(currentBuffer)).ToList();
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
            IoCManager.Resolve<IBaseServer>().Shutdown();
        }
    }
}
