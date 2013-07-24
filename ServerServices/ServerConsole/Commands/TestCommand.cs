using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerServices.ServerConsole.Commands
{
    public class TestCommand : ConsoleCommand
    {
        public override string GetCommand()
        {
            return "test";
        }

        public override void Execute(params object[] args)
        {
            Console.WriteLine("Test!");
        }
    }
}
