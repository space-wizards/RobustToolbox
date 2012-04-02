using Lidgren.Network;
namespace ServerInterfaces.GameObject
{
    public interface IEntityManager
    {
        void Shutdown();
        void HandleEntityNetworkMessage(NetIncomingMessage message);
        void HandleNetworkMessage(NetIncomingMessage message);
        IEntity GetEntity(int id);
        void DeleteEntity(IEntity entity);
        void SendEntities(NetConnection connection);
        void SaveEntities();
        IEntity SpawnEntity(string template, bool send = true);
        IComponentFactory ComponentFactory { get; }
    }
}
