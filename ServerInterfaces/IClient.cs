using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using Lidgren.Network;

namespace ServerInterfaces
{
    public interface IClient
    {
        NetConnection NetConnection { get; }
        string PlayerName { get; set; }
        ClientStatus Status { get; set; }
        ushort MobID { get; set; }
    }
}
