
using ServerInterfaces;

namespace ServerServices
{
    public class SS13Server : ISS13Server
    {
        private ISS13Server instance;

        public void MainLoop()
        {
            instance.MainLoop();
        }

        public ServerInterfaces.GOC.IEntityManager EntityManager
        {
            get { return instance.EntityManager; }
        }

        public IClient GetClient(Lidgren.Network.NetConnection clientConnection)
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

        public SS13_Shared.ServerEnums.RunLevel Runlevel
        {
            get { return instance.Runlevel; }
        }

        public void Restart()
        {
            instance.Restart();
        }

        public ServerInterfaces.Map.IMapManager GetMap()
        {
            return instance.GetMap();
        }

        public void SetServerInstance(ISS13Server server)
        {
            instance = server;
        }
    }
}
