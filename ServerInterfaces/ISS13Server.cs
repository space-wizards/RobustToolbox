using Lidgren.Network;
using ServerInterfaces.GameObject;
using SS13_Shared.ServerEnums;
using ServerInterfaces.Map;
namespace ServerInterfaces
{
    public interface ISS13Server
    {
        IEntityManager EntityManager { get; }
        IClient GetClient(NetConnection clientConnection);
        void SaveMap();
        void SaveEntities();
        RunLevel Runlevel { get; }

        IMapManager GetMap();
    }
}
