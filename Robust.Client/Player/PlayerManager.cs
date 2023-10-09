using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Client.Player
{
    /// <summary>
    ///     Here's the player controller. This will handle attaching GUIs and input to controllable things.
    ///     Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated.
    ///     This class also communicates with the server to let the server control what entity it is attached to.
    /// </summary>
    internal sealed class PlayerManager : SharedPlayerManager, IPlayerManager
    {
        [Dependency] private readonly IClientNetManager _network = default!;
        [Dependency] private readonly IBaseClient _client = default!;

        /// <summary>
        /// Received player states that had an unknown <see cref="NetEntity"/>.
        /// </summary>
        private Dictionary<NetUserId, SessionState> _pendingStates = new ();
        private List<SessionState> _pending = new();

        /// <inheritdoc />
        public override ICommonSession[] NetworkedSessions
        {
            get
            {
                return LocalSession != null
                    ? new [] { LocalSession }
                    : Array.Empty<ICommonSession>();
            }
        }

        /// <inheritdoc />
        public override int MaxPlayers => _client.GameInfo?.ServerMaxPlayers ?? 0;

        public LocalPlayer? LocalPlayer { get; set; }

        public event Action<SessionStatusEventArgs>? LocalStatusChanged;
        public event Action? PlayerListUpdated;
        public event Action<EntityUid>? LocalPlayerDetached;
        public event Action<EntityUid>? LocalPlayerAttached;

        /// <inheritdoc />
        public override void Initialize(int maxPlayers)
        {
            base.Initialize(maxPlayers);
            _network.RegisterNetMessage<MsgPlayerListReq>();
            _network.RegisterNetMessage<MsgPlayerList>(HandlePlayerList);
            PlayerStatusChanged += StatusChanged;
        }

        private void StatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.Session == LocalPlayer?.Session)
                LocalStatusChanged?.Invoke(e);
        }

        /// <inheritdoc />
        public override void Startup()
        {
            if (LocalSession == null)
                throw new InvalidOperationException("LocalSession cannot be null");

            LocalPlayer = new LocalPlayer(LocalSession);
            base.Startup();
        }

        public void SetupSinglePlayer(string name)
        {
            if (LocalSession != null)
                throw new InvalidOperationException($"Player manager already running?");

            LocalSession = CreateAndAddSession(default, name);
            Startup();
            PlayerListUpdated?.Invoke();
        }

        public void SetupMultiplayer(INetChannel channel)
        {
            if (LocalSession != null)
                throw new InvalidOperationException($"Player manager already running?");

            var session = CreateAndAddSession(channel.UserId, channel.UserName);
            session.Channel = channel;
            LocalSession = session;
            Startup();
            _network.ClientSendMessage(new MsgPlayerListReq());
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            if (LocalSession != null)
                SetAttachedEntity(LocalSession, null);
            LocalPlayer = null;
            LocalSession = null;
            _pendingStates.Clear();
            base.Shutdown();
            PlayerListUpdated?.Invoke();
        }

        public override void SetAttachedEntity(ICommonSession session, EntityUid? uid)
        {
            if (session.AttachedEntity == uid)
                return;

            var old = session.AttachedEntity;
            base.SetAttachedEntity(session, uid);

            if (session != LocalSession)
                return;

            if (old.HasValue)
            {
                Sawmill.Info($"Detaching local player from {EntManager.ToPrettyString(old)}.");
                EntManager.EventBus.RaiseLocalEvent(old.Value, new LocalPlayerDetachedEvent(old.Value), true);
                LocalPlayerDetached?.Invoke(old.Value);
            }

            if (uid == null)
            {
                Sawmill.Info($"Local player is no longer attached to any entity.");
                return;
            }

            if (!EntManager.EntityExists(uid))
            {
                Sawmill.Error($"Attempted to attach player to non-existent entity {uid}!");
                return;
            }

            if (!EntManager.EnsureComponent(uid.Value, out EyeComponent eye))
            {
                if (_client.RunLevel != ClientRunLevel.SinglePlayerGame)
                    Sawmill.Warning($"Attaching local player to an entity {EntManager.ToPrettyString(uid)} without an eye. This eye will not be netsynced and may cause issues.");
                eye.NetSyncEnabled = false;
            }

            Sawmill.Info($"Attaching local player to {EntManager.ToPrettyString(uid)}.");
            EntManager.EventBus.RaiseLocalEvent(uid.Value, new LocalPlayerAttachedEvent(uid.Value), true);
            LocalPlayerAttached?.Invoke(uid.Value);
        }

        public void ApplyPlayerStates(IReadOnlyCollection<SessionState> list)
        {
            var dirty = ApplyStates(list, true);

            if (_pendingStates.Count == 0)
            {
                // This is somewhat inefficient as it might try to re-apply states that failed just a moment ago.
                _pending.Clear();
                _pending.AddRange(_pendingStates.Values);
                _pendingStates.Clear();
                dirty |= ApplyStates(_pending, false);
            }

            if (dirty)
                PlayerListUpdated?.Invoke();
        }

        private bool ApplyStates(IReadOnlyCollection<SessionState> list, bool fullList)
        {
            if (list.Count == 0)
                return false;

            DebugTools.Assert(_network.IsConnected ||  _client.RunLevel == ClientRunLevel.SinglePlayerGame // replays use state application.
                , "Received player state without being connected?");
            DebugTools.Assert(LocalSession != null, "Received player state before Session finished setup.");

            var state = list.FirstOrDefault(s => s.UserId == LocalSession.UserId);

            bool dirty = false;
            if (state != null)
            {
                dirty = true;
                if (!EntManager.TryGetEntity(state.ControlledEntity, out var uid)
                    && state.ControlledEntity is { Valid:true } )
                {
                    Sawmill.Error($"Received player state for local player with an unknown net entity!");
                    _pendingStates[state.UserId] = state;
                }
                else
                {
                    _pendingStates.Remove(state.UserId);
                }

                SetAttachedEntity(LocalSession, uid);
                SetStatus(LocalSession, state.Status);
            }

            return UpdatePlayerList(list, fullList) || dirty;
        }

        /// <summary>
        ///     Handles the incoming PlayerList message from the server.
        /// </summary>
        private void HandlePlayerList(MsgPlayerList msg)
        {
            ApplyPlayerStates(msg.Plyrs);
        }

        /// <summary>
        ///     Compares the server player list to the client one, and updates if needed.
        /// </summary>
        private bool UpdatePlayerList(IEnumerable<SessionState> remotePlayers, bool fullList)
        {
            var dirty = false;
            var users = new List<NetUserId>();
            foreach (var state in remotePlayers)
            {
                users.Add(state.UserId);

                if (!EntManager.TryGetEntity(state.ControlledEntity, out var controlled)
                    && state.ControlledEntity is {Valid: true})
                {
                    _pendingStates[state.UserId] = state;
                }
                else
                {
                    _pendingStates.Remove(state.UserId);
                }

                if (!InternalSessions.TryGetValue(state.UserId, out var session))
                {
                    // This is a new userid, so we create a new session.
                    DebugTools.Assert(state.UserId != LocalPlayer?.UserId);
                    var newSession = CreateAndAddSession(state.UserId, state.Name);
                    newSession.Ping = state.Ping;
                    newSession.Name = state.Name;
                    SetStatus(newSession, state.Status);
                    SetAttachedEntity(newSession, controlled);
                    dirty = true;
                    continue;
                }

                // Check if the data is actually different
                if (session.Name == state.Name
                    && session.Status == state.Status
                    && session.Ping == state.Ping
                    && session.AttachedEntity == controlled)
                {
                    continue;
                }

                dirty = true;
                var local = (CommonSession) session;
                local.Name = state.Name;
                local.Ping = state.Ping;
                SetStatus(local, state.Status);
                SetAttachedEntity(local, controlled);
            }

            // Remove old users. This only works if the provided state is a list of all players
            if (fullList)
            {
                foreach (var oldUser in InternalSessions.Keys.ToArray())
                {
                    // clear slot, player left
                    if (users.Contains(oldUser))
                        continue;

                    DebugTools.Assert(oldUser != LocalUser
                                      || LocalUser == null
                                      || LocalUser == default(NetUserId),
                        "Client is still connected to the server but not in the list of players?");
                    RemoveSession(oldUser);
                    _pendingStates.Remove(oldUser);
                    dirty = true;
                }
            }

            return dirty;
        }

        public override bool TryGetSessionByEntity(EntityUid uid, [NotNullWhen(true)] out ICommonSession? session)
        {
            if (LocalEntity == uid)
            {
                session = LocalSession!;
                return true;
            }

            session = null;
            return false;
        }
    }
}
