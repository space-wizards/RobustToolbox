using System;
using SFML.System;
using SS14.Shared.GameStates;
using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.ServerEnums;

namespace SS14.Server.Interfaces.Player
{
    /// <summary>
    /// Manages each players session when connected to the server.
    /// </summary>
    public interface IPlayerManager
    {
        RunLevel RunLevel { get; set; }

        int PlayerCount { get; }

        void SpawnPlayerMob(IPlayerSession session);

        [Obsolete("Use GetSessionById")]
        IPlayerSession GetSessionByConnection(NetConnection senderConnection);

        IPlayerSession GetSessionById(int networkID);
        
        IPlayerSession GetSessionByChannel(INetChannel channel);

        void Initialize(BaseServer baseServer);

        void SendJoinGameToAll();

        void SendJoinLobbyToAll();

        void HandleNetworkMessage(MsgSession msg);

        void DetachAll();
        List<IPlayerSession> GetPlayersInLobby();
        List<IPlayerSession> GetPlayersInRange(Vector2f position, int range);
        List<IPlayerSession> GetAllPlayers();
        List<PlayerState> GetPlayerStates();
    }
}
