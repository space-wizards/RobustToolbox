using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Client.Player
{
    /// <summary>
    ///     Here's the player controller. This will handle attaching GUIs and input to controllable things.
    ///     Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated.
    ///     This class also communicates with the server to let the server control what entity it is attached to.
    /// </summary>
    public class PlayerManager : IPlayerManager
    {
        //private readonly List<PostProcessingEffect> _effects = new List<PostProcessingEffect>();

        [Dependency]
        private readonly IClientNetManager _network;

        [Dependency]
        private readonly IBaseClient _client;

        [Dependency]
        private readonly IConfigurationManager _config;

        [Dependency]
        private readonly IEntityManager _entityManager;

        /// <summary>
        ///     Active sessions of connected clients to the server.
        /// </summary>
        private Dictionary<int, PlayerSession> _sessions;
        
        /// <inheritdoc />
        public int PlayerCount => _sessions.Values.Count;

        /// <inheritdoc />
        public int MaxPlayers => _client.GameInfo.ServerMaxPlayers;

        /// <inheritdoc />
        public LocalPlayer LocalPlayer { get; private set; }

        /// <inheritdoc />
        public IEnumerable<PlayerSession> Sessions => _sessions.Values;

        /// <inheritdoc />
        public IReadOnlyDictionary<int, PlayerSession> SessionsDict => _sessions;

        /// <inheritdoc />
        public event EventHandler PlayerListUpdated;

        /// <inheritdoc />
        public void Initialize()
        {
            _sessions = new Dictionary<int, PlayerSession>();

            _config.RegisterCVar("player.name", "Joe Genero", CVar.ARCHIVE);

            _network.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME);

            _network.RegisterNetMessage<MsgPlayerList>(MsgPlayerList.NAME, HandlePlayerList);

            _network.RegisterNetMessage<MsgSession>(MsgSession.NAME, HandleSessionMessage);

            _network.RegisterNetMessage<MsgClGreet>(MsgClGreet.NAME);
        }

        /// <inheritdoc />
        public void Startup(INetChannel channel)
        {
            LocalPlayer = new LocalPlayer(_network, _config);

            var msgList = _network.CreateNetMessage<MsgPlayerListReq>();
            // message is empty
            _network.ClientSendMessage(msgList);
        }

        /// <inheritdoc />
        public void Update(float frameTime)
        {
            //foreach (var e in _effects.ToArray())
            //{
            //    e.Update(frameTime);
            //}
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            LocalPlayer = null;
            _sessions.Clear();
        }

        /// <inheritdoc />
        public void Destroy() { }

        /*
        /// <inheritdoc />
        public void ApplyEffects(RenderImage image)
        {
            foreach (var e in _effects)
            {
                e.ProcessImage(image);
            }
        }
        */

        /// <inheritdoc />
        public void ApplyPlayerStates(List<PlayerState> list)
        {
            Debug.Assert(_network.IsConnected, "Received player state without being connected?");
            Debug.Assert(LocalPlayer != null, "Call Startup()");
            Debug.Assert(LocalPlayer.Session != null, "Received player state before Session finished setup.");

            var myState = list.FirstOrDefault(s => s.Index == LocalPlayer.Index);

            if (myState != null)
            {
                UpdateAttachedEntity(myState.ControlledEntity);
                UpdateSessionStatus(myState.Status);
            }

            UpdatePlayerList(list);
        }

        /// <summary>
        ///     Handles an incoming session NetMsg from the server.
        /// </summary>
        private void HandleSessionMessage(NetMessage netMessage)
        {
            var msg = (MsgSession)netMessage;

            switch (msg.MsgType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    break;
                case PlayerSessionMessage.JoinLobby:
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    //AddEffect(msg.PpType, msg.PpDuration);
                    break;
            }
        }

        /// <summary>
        ///     Compares the server sessionStatus to the client one, and updates if needed.
        /// </summary>
        private void UpdateSessionStatus(SessionStatus myStateStatus)
        {
            if (LocalPlayer.Session.Status != myStateStatus)
                LocalPlayer.SwitchState(myStateStatus);
        }

        /// <summary>
        ///     Compares the server attachedEntity to the client one, and updates if needed.
        /// </summary>
        /// <param name="entity">AttachedEntity in the server session.</param>
        private void UpdateAttachedEntity(EntityUid? entity)
        {
            if (entity != null &&
                (LocalPlayer.ControlledEntity == null ||
                 LocalPlayer.ControlledEntity != null && entity != LocalPlayer.ControlledEntity.Uid))
                LocalPlayer.AttachEntity(
                    _entityManager.GetEntity(entity.Value));
        }

        /// <summary>
        ///     Handles the incoming PlayerList message from the server.
        /// </summary>
        private void HandlePlayerList(NetMessage netMessage)
        {
            //update sessions with player info
            var msg = (MsgPlayerList)netMessage;

            UpdatePlayerList(msg.Plyrs);
        }

        /// <summary>
        ///     Compares the server player list to the client one, and updates if needed.
        /// </summary>
        private void UpdatePlayerList(List<PlayerState> remotePlayers)
        {
            var dirty = false;

            // diff the sessions to the states
            for (var i = 0; i < MaxPlayers; i++)
            {
                // try to get local session
                var cSession = _sessions.ContainsKey(i) ? _sessions[i] : null;

                // should these be mapped NetId -> PlyInfo?
                var info = remotePlayers.FirstOrDefault(state => state.Index == i);

                // slot already occupied
                if (cSession != null && info != null)
                {
                    if (info.Uuid != cSession.Uuid) // not the same player
                    {
                        Debug.Assert(LocalPlayer.Index != info.Index, "my uuid should not change");
                        dirty = true;

                        _sessions.Remove(info.Index);
                        var newSession = new PlayerSession(info.Index, info.Uuid);
                        newSession.Name = info.Name;
                        newSession.Status = info.Status;
                        newSession.Ping = info.Ping;
                        _sessions.Add(info.Index, newSession);
                    }
                    else // same player, update info
                    {
                        if (cSession.Name == info.Name && cSession.Status == info.Status && cSession.Ping == info.Ping)
                            continue;

                        dirty = true;
                        cSession.Name = info.Name;
                        cSession.Status = info.Status;
                        cSession.Ping = info.Ping;
                    }
                }
                // clear slot, player left
                else if (cSession != null)
                {
                    Debug.Assert(LocalPlayer.Index != i, "I'm still connected to the server, but i left?");
                    dirty = true;

                    _sessions.Remove(cSession.Index);
                }

                // add new session to slot
                else if (info != null)
                {
                    Debug.Assert(LocalPlayer.Index != info.Index || LocalPlayer.Session == null, "I already have a session, why am i getting a new one?");
                    dirty = true;

                    var newSession = new PlayerSession(info.Index, info.Uuid);
                    newSession.Name = info.Name;
                    newSession.Status = info.Status;
                    newSession.Ping = info.Ping;
                    _sessions.Add(info.Index, newSession);

                    if (LocalPlayer.Index == info.Index)
                        LocalPlayer.Session = newSession;
                }
                // else they are both null, continue
            }

            //raise event
            if (dirty)
                PlayerListUpdated?.Invoke(this, EventArgs.Empty);
        }
        /*
        private void AddEffect(PostProcessingEffectType type, float duration)
        {
            PostProcessingEffect e;
            switch (type)
            {
                case PostProcessingEffectType.Blur:
                    e = new BlurPostProcessingEffect(duration);
                    e.OnExpired += EffectExpired;
                    _effects.Add(e);
                    break;
                case PostProcessingEffectType.Death:
                    e = new DeathPostProcessingEffect(duration);
                    e.OnExpired += EffectExpired;
                    _effects.Add(e);
                    break;
            }
        }

        private void EffectExpired(PostProcessingEffect effect)
        {
            effect.OnExpired -= EffectExpired;
            if (_effects.Contains(effect))
                _effects.Remove(effect);
        }
        */
    }
}
