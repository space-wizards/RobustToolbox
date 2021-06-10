using System;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;

namespace Robust.Server.Player
{
    /// <summary>
    /// This is the session of a connected client.
    /// </summary>
    internal class PlayerSession : IPlayerSession
    {
        private readonly PlayerManager _playerManager;
        public readonly PlayerState PlayerState;

        public PlayerSession(PlayerManager playerManager, INetChannel client, PlayerData data)
        {
            _playerManager = playerManager;
            UserId = client.UserId;
            Name = client.UserName;
            _data = data;

            PlayerState = new PlayerState
            {
                UserId = client.UserId,
            };

            ConnectedClient = client;

            UpdatePlayerState();
        }

        [ViewVariables] public INetChannel ConnectedClient { get; }

        [ViewVariables] public IEntity? AttachedEntity { get; set; }

        [ViewVariables] public EntityUid? AttachedEntityUid => AttachedEntity?.Uid;

        private SessionStatus _status = SessionStatus.Connecting;

        /// <inheritdoc />

        [ViewVariables]
        internal string Name { get; set; }

        /// <inheritdoc />
        string ICommonSession.Name
        {
            get => this.Name;
            set => this.Name = value;
        }

        [ViewVariables]
        internal short Ping
        {
            get => ConnectedClient.Ping;
            set => throw new NotSupportedException();
        }

        short ICommonSession.Ping
        {
            get => this.Ping;
            set => this.Ping = value;
        }

        [ViewVariables]
        internal SessionStatus Status
        {
            get => _status;
            set
            {
                if (_status == value)
                    return;

                var old = _status;
                _status = value;
                UpdatePlayerState();

                PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(this, old, value));
            }
        }

        /// <inheritdoc />
        SessionStatus ICommonSession.Status
        {
            get => this.Status;
            set => this.Status = value;
        }

        /// <inheritdoc />
        public DateTime ConnectedTime { get; private set; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public int VisibilityMask { get; set; } = 1;

        /// <inheritdoc />
        [ViewVariables]
        public NetUserId UserId { get; }

        private readonly PlayerData _data;
        [ViewVariables] public IPlayerData Data => _data;

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs>? PlayerStatusChanged;

        /// <inheritdoc />
        public void AttachToEntity(IEntity? entity)
        {
            DetachFromEntity();

            if (entity == null)
                return;

            // This event needs to be broadcast.
            var attachPlayer = new AttachPlayerEvent(entity, this);
            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, attachPlayer);

            if (!attachPlayer.Result)
            {
                Logger.Warning($"Couldn't attach player \"{this}\" to entity \"{entity}\"! Did it have a player already attached to it?");
            }
        }

        /// <inheritdoc />
        public void DetachFromEntity()
        {
            if (AttachedEntity == null)
                return;

#if EXCEPTION_TOLERANCE
            if (AttachedEntity.Deleted)
            {
                Logger.Warning($"Player \"{this}\" was attached to an entity that was deleted. THIS SHOULD NEVER HAPPEN, BUT DOES.");
                // We can't contact ActorSystem because trying to fire an entity event would crash.
                // Work around it.
                ((IPlayerSession) this).SetAttachedEntity(null);
                return;
            }
#endif

            var detachPlayer = new DetachPlayerEvent();
            AttachedEntity.EntityManager.EventBus.RaiseLocalEvent(AttachedEntity.Uid, detachPlayer, false);

            if (!detachPlayer.Result)
            {
                Logger.Warning($"Couldn't detach player \"{this}\" to entity \"{AttachedEntity}\"! Is it missing an ActorComponent?");
            }
        }

        /// <inheritdoc />
        public void OnConnect()
        {
            ConnectedTime = DateTime.UtcNow;
            Status = SessionStatus.Connected;
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void OnDisconnect()
        {
            Status = SessionStatus.Disconnected;

            DetachFromEntity();
            UpdatePlayerState();
        }

        private void SetAttachedEntityName()
        {
            if (Name != null && AttachedEntity != null)
            {
                AttachedEntity.Name = Name;
            }
        }

        /// <summary>
        ///     Causes the session to switch from the lobby to the game.
        /// </summary>
        public void JoinGame()
        {
            if (ConnectedClient == null || Status == SessionStatus.InGame)
                return;

            Status = SessionStatus.InGame;
            UpdatePlayerState();
        }

        public LoginType AuthType => ConnectedClient.AuthType;

        /// <inheritdoc />
        void IPlayerSession.SetAttachedEntity(IEntity? entity)
        {
            AttachedEntity = entity;
            UpdatePlayerState();
        }

        private void UpdatePlayerState()
        {
            PlayerState.Status = Status;
            PlayerState.Name = Name;
            if (AttachedEntity == null)
                PlayerState.ControlledEntity = null;
            else
                PlayerState.ControlledEntity = AttachedEntity.Uid;

            _playerManager.Dirty();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
