using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace ServerInterfaces
{
    public interface IPlayerSession
    {
        NetConnection ConnectedClient { get; }
    }
}
