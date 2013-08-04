using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces;

namespace SS13_Server.Modules.Client
{
    public class Client : IClient
    {
        public Client(NetConnection connection)
        {
            NetConnection = connection;
        }

        #region IClient Members

        public NetConnection NetConnection { get; private set; }
        public string PlayerName { get; set; }
        public ClientStatus Status { get; set; }
        public ushort MobID { get; set; }

        #endregion
    }
}