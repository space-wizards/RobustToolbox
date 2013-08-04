using Lidgren.Network;
using SS13_Shared.ServerEnums;
using ServerInterfaces.GOC;
using ServerInterfaces.Map;

namespace ServerInterfaces
{
    public interface ISS13Server
    {
        IEntityManager EntityManager { get; }
        RunLevel Runlevel { get; }
        void SetServerInstance(ISS13Server server);
        void MainLoop();
        IClient GetClient(NetConnection clientConnection);
        void SaveMap();
        void SaveEntities();
        void Restart();
        IMapManager GetMap();
    }
}