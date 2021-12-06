using System;
using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

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

        /// <inheritdoc />
        [ViewVariables] public EntityUid? AttachedEntity { get; set; }

        private SessionStatus _status = SessionStatus.Connecting;

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
        public void AttachToEntity(EntityUid? entity)
        {
            DetachFromEntity();

            if (entity == null)
                return;

            AttachToEntity((EntityUid) entity);
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
            if (AttachedEntity == null)
                return;

#if EXCEPTION_TOLERANCE
            if (!IoCManager.Resolve<IEntityManager>().EntityExists(AttachedEntityUid!.Value))
            {
                Logger.Warning($"Player \"{this}\" was attached to an entity that was deleted. THIS SHOULD NEVER HAPPEN, BUT DOES.");
                // We can't contact ActorSystem because trying to fire an entity event would crash.
                // Work around it.
                AttachedEntityUid = null;
                UpdatePlayerState();
                return;
            }
#endif

            if (!EntitySystem.Get<ActorSystem>().Detach(AttachedEntity.Value))
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
                IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(AttachedEntity.Value).EntityName = Name;
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
        void IPlayerSession.SetAttachedEntity(EntityUid entity)
        {
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
            PlayerState.ControlledEntity = AttachedEntity;

            _playerManager.Dirty();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
