using SS14.Server.Interfaces.Configuration;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Server.Interfaces;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Con = System.Console;

namespace SS14.Server.Services.ServerConsole
{
    public class ConsoleManager : IConsoleManager
    {
        private readonly Dictionary<string, ConsoleCommand> availableCommands = new Dictionary<string, ConsoleCommand>();
        private readonly Dictionary<int, string> commandHistory = new Dictionary<int, string>();
        private string currentBuffer = "";
        private int historyIndex;
        private int internalCursor;
        private string tabCompleteBuffer = "";
        private List<string> tabCompleteList = new List<string>();
        private int tabCompleteIndex;
        private ConsoleKey lastKeyPressed = ConsoleKey.NoName;

        public ConsoleManager()
        {
            InitializeCommands();
            var consoleSize = IoCManager.Resolve<IServerConfigurationManager>().ConsoleSize;
            try
            {
                Con.SetWindowSize(consoleSize.X, consoleSize.Y);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Con.WriteLine("Resizing Failure:");
                Con.WriteLine(e.Message);
            }

            Console.CancelKeyPress += CancelKeyHandler;
        }

        #region IConsoleManager Members

        public void Update()
        {
            //Process keyboard input
            if (Con.KeyAvailable)
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
                                //currentBuffer = currentBuffer.Substring(0, currentBuffer.Length - 1);
                                internalCursor--;
                            }
                            break;
                        case ConsoleKey.Delete:
                            if (currentBuffer.Length > 0 && internalCursor < currentBuffer.Length)
                            {
                                currentBuffer = currentBuffer.Remove(internalCursor, 1);
                                //internalCursor--;
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
                                tabCompleteBuffer = currentBuffer;
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

        #endregion

        private void ExecuteCommand(string commandLine)
        {
            string[] args = commandLine.Split(' ');
            if (args.Length == 0)
            {
                return;
            }
            string cmd = args[0].ToLower();
            bool handled = false;
            switch (cmd)
            {
                case "list":
                    ListCommands();
                    handled = true;
                    break;
                case "help":
                    if (args.Length > 1 && args[1].Length > 0)
                        HelpCommand(args[1]);
                    else
                    {
                        Help();
                        ListCommands();
                    }
                    handled = true;
                    break;
            }
            if (handled)
                return;
            if (availableCommands.ContainsKey(cmd))
                availableCommands[cmd].Execute(args);
            else if (cmd.Length == 0)
                return;
            else
                Con.WriteLine("Unknown command: " + cmd);
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

        private void InitializeCommands()
        {
            var CommandTypes = new List<Type>();
            CommandTypes.AddRange(
                Assembly.GetCallingAssembly().GetTypes().Where(t => typeof(ConsoleCommand).IsAssignableFrom(t)));
            foreach (Type t in CommandTypes)
            {
                if (t == typeof(ConsoleCommand))
                    continue;
                var instance = Activator.CreateInstance(t, null) as ConsoleCommand;
                RegisterCommand(instance);
            }
        }

        private void RegisterCommand(ConsoleCommand commandObj)
        {
            if (!availableCommands.ContainsKey(commandObj.Command.ToLower()))
                availableCommands.Add(commandObj.Command.ToLower(), commandObj);
        }

        private void ListCommands()
        {
            Con.ForegroundColor = ConsoleColor.Yellow;
            Con.WriteLine("\nAvailable commands:\n");

            List<string> names = availableCommands.Keys.ToList();
            names.Add("list");
            names.Add("help");
            names.Sort();
            foreach (string c in names)
            {
                string name = String.Format("{0, 16}", c);
                Con.ForegroundColor = ConsoleColor.Cyan;
                Con.SetCursorPosition(0, Console.CursorTop);
                Con.Write(name);
                Con.ForegroundColor = ConsoleColor.Green;
                Con.Write(" - ");
                Con.ForegroundColor = ConsoleColor.White;
                switch (c)
                {
                    case "list":
                        Con.WriteLine("Lists available commands");
                        break;
                    case "help":
                        Con.WriteLine("Lists general help. Type 'help <command>' for specific help on a command.");
                        break;
                    default:
                        Con.WriteLine(availableCommands[c].Description);
                        break;
                }
                Con.ResetColor();
            }
            Con.ForegroundColor = ConsoleColor.White;
            Con.Write("\n\t\t\t" + availableCommands.Count);
            Con.ForegroundColor = ConsoleColor.Yellow;
            Con.WriteLine(" commands available.\n");
            Con.ResetColor();
        }

        private void Help()
        {
            Con.ForegroundColor = ConsoleColor.White;
            Con.WriteLine("Help!");
        }

        private void HelpCommand(params string[] args)
        {
            if (availableCommands.ContainsKey(args[0].ToLower()))
            {
                Con.WriteLine("Help for " + args[0] + ":");
                Con.WriteLine(availableCommands[args[0].ToLower()].Help);
            }
            else
            {
                Con.WriteLine("Command '" + args[0] + "' not found.");
            }
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
            IoCManager.Resolve<ISS14Server>().Shutdown();
        }
    }
}
