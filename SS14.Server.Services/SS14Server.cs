using Lidgren.Network;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Map;
using SS14.Shared.ServerEnums;

namespace SS14.Server.Services
{
    public class SS14Server : ISS14Server
    {
        private ISS14Server instance;

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

        public void Shutdown(string reason=null)
        {
            instance.Shutdown(reason);
        }

        public IMapManager GetMap()
        {
            return instance.GetMap();
        }

        public void SetServerInstance(ISS14Server server)
        {
            instance = server;
        }

        #endregion
    }
}
