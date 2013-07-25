using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13.IoC;
using ServerInterfaces.Player;

namespace ServerServices.ServerConsole.Commands
{
    public class ListPlayers : ConsoleCommand
    {
        public override string Command
        {
            get { return "listplayers"; }
        }

        public override string Description
        {
            get { return "Lists all players currently connected"; }
        }

        public override string Help
        {
            get { return "Usage: listplayers"; }
        }

        public override void Execute(params string[] args)
        {
            var players = IoCManager.Resolve<IPlayerManager>().GetAllPlayers();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Current Players:\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0, 20}{1,16}{2,12}{3, 14}{4,9}", "Player Name", "IP Address", "Status", "Playing Time", "Ping");
            foreach(var p in players)
            {
                Console.Write("{0, 20}", p.name);
                Console.WriteLine("{0,16}{1,12}{2,14}{3,9}",
                    p.connectedClient.RemoteEndPoint.Address, 
                    p.status.ToString(), 
                    (DateTime.Now - p.ConnectedTime).ToString(@"hh\:mm\:ss"),
                    Math.Round(p.connectedClient.AverageRoundtripTime * 1000, 2) + "ms");
            }
        }
    }
}
