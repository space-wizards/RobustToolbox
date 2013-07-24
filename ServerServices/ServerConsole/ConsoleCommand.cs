using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerServices.ServerConsole
{
    public abstract class ConsoleCommand
    {
        public abstract string GetCommand();
        public abstract void Execute(params object[] args);
    }
}
