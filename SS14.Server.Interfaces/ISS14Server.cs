using Lidgren.Network;
using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Map;
using SS14.Shared.ServerEnums;

namespace SS14.Server.Interfaces
{
    public interface ISS14Server
    {
        IEntityManager EntityManager { get; }
        RunLevel Runlevel { get; }
        void SetServerInstance(ISS14Server server);
        void MainLoop();
        IClient GetClient(NetConnection clientConnection);
        void SaveMap();
        void SaveEntities();
        void Restart();
        IMapManager GetMap();
    }
}
