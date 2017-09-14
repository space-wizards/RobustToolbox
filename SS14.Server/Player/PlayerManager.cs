using System;
using OpenTK;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.ServerEnums;
using SS14.Shared.Utility;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Server.Player
{
    /// <summary>
    /// This class will manage connected player sessions.
    /// </summary>
    public class PlayerManager : IPlayerManager
    {
        /// <summary>
        /// The server that instantiated this manager.
        /// </summary>
        public IBaseServer Server;

        private readonly Dictionary<int, PlayerSession> _sessions;
        private readonly IServerEntityManager _entityManager;

        /// <summary>
        /// Constructs an instance of the player manager.
        /// </summary>
        public PlayerManager()
        {
            _entityManager = IoCManager.Resolve<IServerEntityManager>();
            _sessions = new Dictionary<int, PlayerSession>();
        }

        public int PlayerCount => _sessions.Count;

        /// <summary>
        /// Initializes the manager.
        /// </summary>
        /// <param name="server">The server that instantiated this manager.</param>
        public void Initialize(BaseServer server)
        {
            Server = server;
            Server.OnRunLevelChanged += RunLevelChanged;

            var netMan = IoCManager.Resolve<IServerNetManager>();

            netMan.Connecting += OnConnecting;
            netMan.Connected += NewSession;
            netMan.Disconnect += EndSession;
        }

        private void RunLevelChanged(RunLevel oldLevel, RunLevel newLevel)
        {
            RunLevel = newLevel;
        }

        #region IPlayerManager Members

        private void OnConnecting(object sender, NetConnectingArgs args)
        {
            if (PlayerCount >= Server.MaxPlayers)
                args.Deny = true;
        }

        /// <summary>
        /// Creates a new session for a client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void NewSession(object sender, NetChannelArgs args)
        {
            var session = new PlayerSession(args.Channel, this);
            _sessions.Add(args.Channel.NetworkId, session);
        }

        /// <summary>
        /// Spawns the players entity.
        /// </summary>
        /// <param name="session"></param>
        public void SpawnPlayerMob(IPlayerSession session)
        {
            //TODO: There's probably a much better place to do this.
            IEntity entity = _entityManager.ForceSpawnEntityAt("HumanMob", new Vector2(0, 0), 1); //TODO: Fix this
            session.AttachToEntity(entity);
        }

        public IPlayerSession GetSessionByChannel(INetChannel client)
        {
            IEnumerable<PlayerSession> sessions =
                from s in _sessions
                where s.Value.ConnectedClient == client
                select s.Value;

            return sessions.FirstOrDefault(); // Should only be one session per client. Returns that session, in theory.
        }

        /// <summary>
        /// Returns the client session of the networkId.
        /// </summary>
        /// <param name="networkId">The network id of the client.</param>
        /// <returns></returns>
        public IPlayerSession GetSessionById(int networkId)
        {
            return _sessions.TryGetValue(networkId, out PlayerSession session) ? session : null;
        }

        [Obsolete("Use GetSessionById/GetSessionByChannel")]
        public IPlayerSession GetSessionByConnection(NetConnection client)
        {
            IEnumerable<PlayerSession> sessions =
                from s in _sessions
                where s.Value.ConnectedClient.Connection == client
                select s.Value;

            return sessions.FirstOrDefault(); // Should only be one session per client. Returns that session, in theory.
        }

        public RunLevel RunLevel { get; set; }

        /// <summary>
        /// Processes an incoming network message.
        /// </summary>
        /// <param name="msg">Incoming message.</param>
        public void HandleNetworkMessage(MsgSession msg)
        {
            GetSessionById(msg.MsgChannel.NetworkId)?.HandleNetworkMessage(msg);
        }

        /// <summary>
        /// Ends a clients session, and disconnects them.
        /// </summary>
        /// <param name="client">Client network channel to close.</param>
        public void EndSession(object sender, NetChannelArgs args)
        {
            var session = GetSessionById(args.Channel.NetworkId);
            if (session == null)
                return; //There is no session!

            //Detach the entity and (don't)delete it.
            session.OnDisconnect();

            _sessions.Remove(args.Channel.NetworkId);
        }

        /// <summary>
        /// Causes all sessions to switch from the lobby to the the game.
        /// </summary>
        public void SendJoinGameToAll()
        {
            foreach (PlayerSession s in _sessions.Values)
            {
                s.JoinGame();
            }
        }

        /// <summary>
        /// Causes all sessions to switch from the game to the lobby.
        /// </summary>
        public void SendJoinLobbyToAll()
        {
            foreach (PlayerSession s in _sessions.Values)
            {
                s.JoinLobby();
            }
        }

        /// <summary>
        /// Causes all sessions to detach from their entity.
        /// </summary>
        public void DetachAll()
        {
            foreach (PlayerSession s in _sessions.Values)
            {
                s.DetachFromEntity();
            }
        }

        /// <summary>
        /// Gets all players inside of a circle.
        /// </summary>
        /// <param name="position">Position of the circle in world-space.</param>
        /// <param name="range">Radius of the circle in world units.</param>
        /// <returns></returns>
        public List<IPlayerSession> GetPlayersInRange(LocalCoordinates position, int range)
        {

            //TODO: This needs to be moved to the PVS system.
            return
                _sessions.Values.Where(x =>
                    x.attachedEntity != null &&
                    position.InRange(x.attachedEntity.GetComponent<ITransformComponent>().LocalPosition, range))
                    .Cast<IPlayerSession>()
                    .ToList();
        }

        /// <summary>
        /// Gets all the players in the game lobby.
        /// </summary>
        /// <returns></returns>
        public List<IPlayerSession> GetPlayersInLobby()
        {
            //TODO: Lobby system needs to be moved to Content Assemblies.
            return
                _sessions.Values.Where(
                    x => x.Status == SessionStatus.InLobby)
                    .Cast<IPlayerSession>()
                    .ToList();
        }

        /// <summary>
        /// Gets all players in the server.
        /// </summary>
        /// <returns></returns>
        public List<IPlayerSession> GetAllPlayers()
        {
            return _sessions.Values.Cast<IPlayerSession>().ToList();
        }

        /// <summary>
        /// Gets all player states in the server.
        /// </summary>
        /// <returns></returns>
        public List<PlayerState> GetPlayerStates()
        {
            return _sessions.Values
                .Select(s => s.PlayerState)
                .ToList();
        }

#endregion IPlayerManager Members
    }
}
