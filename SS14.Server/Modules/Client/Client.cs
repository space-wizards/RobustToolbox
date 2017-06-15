using Lidgren.Network;
using SS14.Server.Interfaces;
using SS14.Shared;

namespace SS14.Server.Modules.Client
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