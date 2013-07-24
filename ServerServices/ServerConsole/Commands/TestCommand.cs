using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerServices.ServerConsole.Commands
{
    public class TestCommand : ConsoleCommand
    {
        public override string Command
        {
            get {return "test";}
            
        }
        public override string Help
        {
            get { return "This is a test command."; }
        }
        public override string Description
        {
            get { return "This is a dummy test command."; }
        }

        public override void Execute(params string[] args)
        {
            Console.WriteLine("Test!");
        }

    }
}
