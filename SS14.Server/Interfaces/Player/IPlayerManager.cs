using System;
using Lidgren.Network;
using SFML.System;
using SS14.Shared.GameStates;
using System.Collections.Generic;
using SS14.Shared.IoC;
using SS14.Shared.Network;
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

        void SpawnPlayerMob(IPlayerSession session);

        [Obsolete("Use GetSessionById")]
        IPlayerSession GetSessionByConnection(NetConnection senderConnection);

        IPlayerSession GetSessionById(int networkID);
        
        IPlayerSession GetSessionByChannel(NetChannel channel);

        void Initialize(BaseServer baseServer);

        void SendJoinGameToAll();

        void SendJoinLobbyToAll();

        void NewSession(NetChannel client);

        void EndSession(NetChannel client);

        void HandleNetworkMessage(MsgSession msg);

        //IPlayerSession GetSessionByIp(string ipKick);

        void DetachAll();
        IEnumerable<IPlayerSession> GetPlayersInLobby();
        IEnumerable<IPlayerSession> GetPlayersInRange(Vector2f position, int range);
        IEnumerable<IPlayerSession> GetAllPlayers();
        List<PlayerState> GetPlayerStates();
    }
}
