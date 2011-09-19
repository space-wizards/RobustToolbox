using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace CGO
{
    class EntityNetworkManager
    {
        private NetClient m_netClient;

        public EntityNetworkManager(NetClient client)
        {
            m_netClient = client;    
        }


    }
}
