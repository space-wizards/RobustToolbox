using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ServerInterfaces.ServerConsole;
using ServerServices.ServerConsole.Commands;
using Con = System.Console;
namespace ServerServices.ServerConsole
{
    public class ConsoleManager : IConsoleManager
    {
        private string currentBuffer = "";
        private Dictionary<int, string> commandHistory = new Dictionary<int, string>();
        private int historyIndex = 0;
        private int internalCursor = 0;
        private Dictionary<string, ConsoleCommand> availableCommands = new Dictionary<string, ConsoleCommand>();

        public ConsoleManager()
        {
            InitializeCommands();
        }

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
                    }
                    DrawCommandLine();
                }
            }
        }

        private void ExecuteCommand(string commandLine)
        {
            var args = commandLine.Split(' ');
            if (args.Length == 0)
            {
                return;
            }
            var cmd = args[0].ToLower();
            var handled = false;
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
                        Help();
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
                Assembly.GetCallingAssembly().GetTypes().Where(t => typeof (ConsoleCommand).IsAssignableFrom(t)));
            foreach (var t in CommandTypes)
            {
                if (t == typeof (ConsoleCommand))
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
            Con.WriteLine("Available commands:");
            foreach (var c in availableCommands)
            {
                Con.WriteLine("\t" + c.Key + "\t" + c.Value.Description);
            }
        }

        private void Help()
        {
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
    }
}
