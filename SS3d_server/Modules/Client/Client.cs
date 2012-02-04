using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Lidgren.Network;

namespace SS13_Server.Modules.Client
{
    public class Client
    {
        public NetConnection netConnection;
        public string playerName = "Player";
        public ClientStatus status;
        public ushort mobID;

        public Client(NetConnection connection)
        {
            netConnection = connection;
        }

        public void SetName(string name)
        {
            name = name.Trim();
            if (name.Length >= 3)
            {
                playerName = name;
            }
        }
    }
}
