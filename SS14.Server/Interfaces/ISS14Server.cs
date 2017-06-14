using Lidgren.Network;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Map;
using SS14.Shared.ServerEnums;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces
{
    public interface ISS14Server : IIoCInterface
    {
        RunLevel Runlevel { get; }
        void SetServerInstance(ISS14Server server);
        void MainLoop();
        IClient GetClient(NetConnection clientConnection);
        void SaveMap();
        void SaveEntities();
        bool Start();
        void Restart();
        void Shutdown(string reason=null);
        IMapManager GetMap();
    }
}
