using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces;

namespace SS13_Server.Modules.Client
{
    public class Client: IClient
    {
        public NetConnection NetConnection { get; private set; }
        public string PlayerName { get; set; }
        public ClientStatus Status { get; set; }
        public ushort MobID { get; set; }

        public Client(NetConnection connection)
        {
            NetConnection = connection;
        }
    }
}
