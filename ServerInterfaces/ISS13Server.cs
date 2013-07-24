using Lidgren.Network;
using ServerInterfaces.GameObject;
using SS13_Shared.ServerEnums;
using ServerInterfaces.Map;
namespace ServerInterfaces
{
    public interface ISS13Server
    {
        void SetServerInstance(ISS13Server server);
        void MainLoop();
        IEntityManager EntityManager { get; }
        IClient GetClient(NetConnection clientConnection);
        void SaveMap();
        void SaveEntities();
        RunLevel Runlevel { get; }
        void Restart();
        IMapManager GetMap();
    }
}
