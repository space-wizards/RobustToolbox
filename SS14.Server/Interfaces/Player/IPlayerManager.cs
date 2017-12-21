using System.Collections.Generic;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;
using SS14.Shared.Network.Messages;
using SS14.Shared.Players;
using SS14.Shared.ServerEnums;

namespace SS14.Server.Interfaces.Player
{
    /// <summary>
    ///     Manages each players session when connected to the server.
    /// </summary>
    public interface IPlayerManager
    {
        //TODO: What is this? Should it be in BaseServer?
        RunLevel RunLevel { get; set; }

        /// <summary>
        ///     Number of players currently connected to this server.
        /// </summary>
        int PlayerCount { get; }

        /// <summary>
        ///     Maximum number of players that can connect to this server at one time.
        /// </summary>
        int MaxPlayers { get; }

        /// <summary>
        ///     Initializes the manager.
        /// </summary>
        /// <param name="baseServer">The server that instantiated this manager.</param>
        /// <param name="maxPlayers">Maximum number of players that can connect to this server at one time.</param>
        void Initialize(BaseServer baseServer, int maxPlayers);

        IPlayerSession GetSessionById(PlayerIndex session);

        IPlayerSession GetSessionByChannel(INetChannel channel);

        //TODO: Move to IPlayerSession
        void SpawnPlayerMob(IPlayerSession session);

        //TODO: These go in BaseServer
        void SendJoinGameToAll();
        void SendJoinLobbyToAll();

        //TODO: Use new networking system.
        void HandleNetworkMessage(MsgSession msg);

        void DetachAll();
        List<IPlayerSession> GetPlayersInLobby();
        List<IPlayerSession> GetPlayersInRange(LocalCoordinates worldPos, int range);
        List<IPlayerSession> GetAllPlayers();
        List<PlayerState> GetPlayerStates();
    }
}
