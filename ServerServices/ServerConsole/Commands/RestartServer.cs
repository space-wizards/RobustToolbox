using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13.IoC;
using ServerInterfaces;

namespace ServerServices.ServerConsole.Commands
{
    public class RestartServer : ConsoleCommand
    {
        public override string GetCommand()
        {
            return "restartserver";
        }

        public override void Execute(params object[] args)
        {
            IoCManager.Resolve<ISS13Server>().Restart();
        }
    }
}
