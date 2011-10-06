using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;

namespace SS3D_Server.Modules
{
    public class SS3DNetServer : NetServer
    {
        private static SS3DNetServer singleton;
        public static SS3DNetServer Singleton
        {
            get
            {
                if (singleton == null)
                    throw new TypeInitializationException("Singleton not initialized.", null);
                return singleton;

            }
        }

        public SS3DNetServer(NetPeerConfiguration netConfig)
            :base(netConfig)
        {
            singleton = this;
        }
    }
}
