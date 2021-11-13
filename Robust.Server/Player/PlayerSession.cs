using System;
using System.Collections.Generic;
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
        private readonly HashSet<EntityUid> _viewSubscriptions = new();

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

        [ViewVariables] public IReadOnlySet<EntityUid> ViewSubscriptions => _viewSubscriptions;

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

            AttachToEntity(entity.Uid);
        }

        /// <inheritdoc />
        public void AttachToEntity(EntityUid uid)
        {
            DetachFromEntity();

            if (!EntitySystem.Get<ActorSystem>().Attach(uid, this))
            {
                Logger.Warning($"Couldn't attach player \"{this}\" to entity \"{uid}\"! Did it have a player already attached to it?");
            }
        }

        /// <inheritdoc />
        public void DetachFromEntity()
        {
            if (AttachedEntityUid == null)
                return;

#if EXCEPTION_TOLERANCE
            if (AttachedEntity!.Deleted)
            {
                Logger.Warning($"Player \"{this}\" was attached to an entity that was deleted. THIS SHOULD NEVER HAPPEN, BUT DOES.");
                // We can't contact ActorSystem because trying to fire an entity event would crash.
                // Work around it.
                AttachedEntity = null;
                UpdatePlayerState();
                return;
            }
#endif

            if (!EntitySystem.Get<ActorSystem>().Detach(AttachedEntityUid.Value))
            {
                Logger.Warning($"Couldn't detach player \"{this}\" from entity \"{AttachedEntity}\"! Is it missing an ActorComponent?");
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

            UnsubscribeAllViews();
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
            // TODO: Use EntityUid for this.
            AttachedEntity = entity;
            UpdatePlayerState();
        }

        void IPlayerSession.AddViewSubscription(EntityUid eye)
        {
            _viewSubscriptions.Add(eye);
        }

        void IPlayerSession.RemoveViewSubscription(EntityUid eye)
        {
            _viewSubscriptions.Remove(eye);
        }

        private void UnsubscribeAllViews()
        {
            var viewSubscriberSystem = EntitySystem.Get<ViewSubscriberSystem>();

            foreach (var eye in _viewSubscriptions)
            {
                viewSubscriberSystem.RemoveViewSubscriber(eye, this);
            }
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
