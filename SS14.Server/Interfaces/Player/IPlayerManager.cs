using Lidgren.Network;
using SFML.System;
using SS14.Shared.GameStates;
using System.Collections.Generic;
using SS14.Shared.IoC;

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
        IPlayerSession[] GetPlayersInRange(Vector2f position, int range);
        IPlayerSession[] GetAllPlayers();
        List<PlayerState> GetPlayerStates();
    }
}
