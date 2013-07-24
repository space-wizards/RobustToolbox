using System;
using System.Collections.Generic;
using System.Linq;
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
                    currentBuffer += key.KeyChar;
                    DrawCommandLine();
                }
                else  {
                    switch(key.Key)
                    {
                        case ConsoleKey.Enter:
                            Con.WriteLine("> " + currentBuffer);
                            commandHistory.Add(commandHistory.Count, currentBuffer);
                            historyIndex = commandHistory.Count;
                            ExecuteCommand(currentBuffer);
                            currentBuffer = "";
                            break;
                        case ConsoleKey.Backspace:
                            if(currentBuffer.Length > 0)
                                currentBuffer = currentBuffer.Substring(0, currentBuffer.Length - 1);
                            break;
                        case ConsoleKey.UpArrow:
                            if (historyIndex > 0) 
                                historyIndex--;
                            if (commandHistory.ContainsKey(historyIndex))
                                currentBuffer = commandHistory[historyIndex];
                            else
                                currentBuffer = "";
                            break;
                        case ConsoleKey.DownArrow:
                            if (historyIndex < commandHistory.Count)
                                historyIndex++;
                            if (commandHistory.ContainsKey(historyIndex))
                                currentBuffer = commandHistory[historyIndex];
                            else
                                currentBuffer = "";
                            break;
                        case ConsoleKey.Escape:
                            historyIndex = commandHistory.Count;
                            currentBuffer = "";
                            break;
                    }
                    DrawCommandLine();
                }
            }  
        }

        private void ExecuteCommand(string commandLine)
        {
            var args = commandLine.Split(' ');
            if(args.Length == 0)
            {return;}
            var cmd = args[0].ToLower();
            if (availableCommands.ContainsKey(cmd))
                availableCommands[cmd].Execute(args);
            else
                Con.WriteLine("Unknown command: " + cmd);

        }

        public void DrawCommandLine()
        {
            ClearCurrentLine();
            Con.SetCursorPosition(0, Con.CursorTop);
            Con.Write("> " + currentBuffer);
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
            RegisterCommand(new TestCommand());
        }

        private void RegisterCommand(ConsoleCommand commandObj)
        {
            if(!availableCommands.ContainsKey(commandObj.GetCommand().ToLower()))
                availableCommands.Add(commandObj.GetCommand().ToLower(), commandObj);
        }

    }
}
