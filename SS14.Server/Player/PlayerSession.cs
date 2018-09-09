using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Server.GameObjects;
using System;
using SS14.Server.Interfaces;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Network;
using SS14.Shared.ViewVariables;

namespace SS14.Server.Player
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
            SessionId = client.SessionId;
            _data = data;

            PlayerState = new PlayerState
            {
                SessionId = client.SessionId,
            };

            ConnectedClient = client;

            UpdatePlayerState();
        }

        [ViewVariables]
        public INetChannel ConnectedClient { get; }

        [ViewVariables]
        public IEntity AttachedEntity { get; private set; }

        [ViewVariables]
        public EntityUid? AttachedEntityUid => AttachedEntity?.Uid;

        private SessionStatus _status = SessionStatus.Connecting;

        /// <inheritdoc />
        public string Name => SessionId.Username;

        /// <inheritdoc />
        [ViewVariables]
        public SessionStatus Status
        {
            get => _status;
            set => OnPlayerStatusChanged(_status, value);
        }

        /// <inheritdoc />
        public DateTime ConnectedTime { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public NetSessionId SessionId { get; }

        readonly PlayerData _data;
        [ViewVariables]
        public IPlayerData Data => _data;

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        private void OnPlayerStatusChanged(SessionStatus oldStatus, SessionStatus newStatus)
        {
            if (oldStatus == newStatus)
                return;

            _status = newStatus;
            UpdatePlayerState();

            PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(this, oldStatus, newStatus));
        }

        /// <inheritdoc />
        public void AttachToEntity(IEntity a)
        {
            DetachFromEntity();

            var actorComponent = a.AddComponent<BasicActorComponent>();
            if (a.HasComponent<IMoverComponent>())
            {
                a.RemoveComponent<IMoverComponent>();
            }
            a.AddComponent<PlayerInputMoverComponent>();

            actorComponent.playerSession = this;

            AttachedEntity = a;
            a.SendMessage(actorComponent, new PlayerAttachedMsg(this));
            SendAttachMessage();
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void DetachFromEntity()
        {
            if (AttachedEntity == null)
            {
                return;
            }

            AttachedEntity.SendMessage(AttachedEntity.GetComponent<BasicActorComponent>(), new PlayerDetachedMsg(this));
            AttachedEntity.RemoveComponent<PlayerInputMoverComponent>();
            AttachedEntity.RemoveComponent<BasicActorComponent>();
            AttachedEntity = null;
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void OnConnect()
        {
            ConnectedTime = DateTime.Now;
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

        /// <inheritdoc />
        public void AddPostProcessingEffect(PostProcessingEffectType type, float duration)
        {
            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgSession>();

            message.MsgType = PlayerSessionMessage.AddPostProcessingEffect;
            message.PpType = type;
            message.PpDuration = duration;

            net.ServerSendMessage(message, ConnectedClient);
        }

        private void SendAttachMessage()
        {
            if (AttachedEntity == null)
                throw new Exception("Cannot attach player session to entity: No entity attached.");

            var net = IoCManager.Resolve<IServerNetManager>();
            var message = net.CreateNetMessage<MsgSession>();

            message.MsgType = PlayerSessionMessage.AttachToEntity;
            message.Uid = AttachedEntity.Uid;

            net.ServerSendMessage(message, ConnectedClient);
        }

        private void SetAttachedEntityName()
        {
            if (Name != null && AttachedEntity != null)
            {
                AttachedEntity.Name = Name;
            }
        }

        /// <inheritdoc />
        public void JoinLobby()
        {
            DetachFromEntity();
            Status = SessionStatus.InLobby;
            UpdatePlayerState();
        }

        /// <summary>
        ///     Causes the session to switch from the lobby to the game.
        /// </summary>
        public void JoinGame()
        {
            var baseServer = IoCManager.Resolve<IBaseServer>();

            if (ConnectedClient == null || Status == SessionStatus.InGame || baseServer.RunLevel != ServerRunLevel.Game)
                return;

            Status = SessionStatus.InGame;
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
            return SessionId.ToString();
        }
    }
}
