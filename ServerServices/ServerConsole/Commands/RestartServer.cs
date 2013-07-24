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
        public override string Command
        {
            get { return "restartserver"; }
        }

        public override void Execute(params string[] args)
        {
            IoCManager.Resolve<ISS13Server>().Restart();
        }

        public override string Description
        {
            get { return "Restarts the server"; }
        }

        public override string Help
        {
            get { return "restartserver:\nRestarts the server."; }
        }
    }
}
