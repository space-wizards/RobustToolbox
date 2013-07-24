using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerServices.ServerConsole
{
    public abstract class ConsoleCommand
    {
        public abstract string Command { get; }
        public abstract string Description { get; }
        public abstract string Help { get; }
        public abstract void Execute(params string[] args);
    }

}
