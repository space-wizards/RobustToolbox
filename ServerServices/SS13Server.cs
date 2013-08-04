using Lidgren.Network;
using SS13_Shared.ServerEnums;
using ServerInterfaces;
using ServerInterfaces.GOC;
using ServerInterfaces.Map;

namespace ServerServices
{
    public class SS13Server : ISS13Server
    {
        private ISS13Server instance;

        #region ISS13Server Members

        public void MainLoop()
        {
            instance.MainLoop();
        }

        public IEntityManager EntityManager
        {
            get { return instance.EntityManager; }
        }

        public IClient GetClient(NetConnection clientConnection)
        {
            return instance.GetClient(clientConnection);
        }

        public void SaveMap()
        {
            instance.SaveMap();
        }

        public void SaveEntities()
        {
            instance.SaveEntities();
        }

        public RunLevel Runlevel
        {
            get { return instance.Runlevel; }
        }

        public void Restart()
        {
            instance.Restart();
        }

        public IMapManager GetMap()
        {
            return instance.GetMap();
        }

        public void SetServerInstance(ISS13Server server)
        {
            instance = server;
        }

        #endregion
    }
}