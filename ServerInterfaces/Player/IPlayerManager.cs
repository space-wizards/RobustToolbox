using Lidgren.Network;

namespace ServerInterfaces.Player
{
    public interface IPlayerManager
    {
        void SpawnPlayerMob(IPlayerSession player);
        IPlayerSession GetSessionByConnection(NetConnection senderConnection);

        void Initialize(ISS13Server server);

        void SendJoinGameToAll();

        void SendJoinLobbyToAll();

        void NewSession(NetConnection sender);

        void EndSession(NetConnection sender);

        void HandleNetworkMessage(NetIncomingMessage msg);

        IPlayerSession GetSessionByIp(string ipKick);

        void DetachAll();
    }
}
