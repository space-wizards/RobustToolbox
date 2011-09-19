using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace SGO
{
    class EntityNetworkManager
    {
        private NetServer m_netServer;
        public EntityNetworkManager(NetServer netServer)
        {
            m_netServer = netServer;
        }
    }
}
