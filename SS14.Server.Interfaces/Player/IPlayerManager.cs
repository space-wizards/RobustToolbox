using Lidgren.Network;
using SS14.Shared.GameStates;
using SS14.Shared.Maths;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerManager
    {
        void SpawnPlayerMob(IPlayerSession player);
        IPlayerSession GetSessionByConnection(NetConnection senderConnection);

        void Initialize(ISS14Server server);

        void SendJoinGameToAll();

        void SendJoinLobbyToAll();

        void NewSession(NetConnection sender);

        void EndSession(NetConnection sender);

        void HandleNetworkMessage(NetIncomingMessage msg);

        IPlayerSession GetSessionByIp(string ipKick);

        void DetachAll();
        IPlayerSession[] GetPlayersInLobby();
        IPlayerSession[] GetPlayersInRange(Vector2 position, int range);
        IPlayerSession[] GetAllPlayers();
        List<PlayerState> GetPlayerStates();
    }
}